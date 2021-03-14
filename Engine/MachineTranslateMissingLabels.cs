using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace label_translator.Engine
{
    public static class MachineTranslateMissingLabels
    {
        private static readonly Dictionary<string, string> _languageTable = new Dictionary<string, string>
        {
            { SupportedLanguages.Default, "en" },
            { SupportedLanguages.Bulgarian, "bg" },
            { SupportedLanguages.Danish, "da" },
            { SupportedLanguages.English, "en" },
            { SupportedLanguages.French, "fr" },
            { SupportedLanguages.German, "de" },
            { SupportedLanguages.Hungarian, "hu" },
            { SupportedLanguages.Italian, "it" },
            { SupportedLanguages.Japanese, "ja" },
            { SupportedLanguages.Polish, "pl" },
            { SupportedLanguages.Portuguese, "pt" },
            { SupportedLanguages.SimplifiedChinese, "zh-HANS" },
            { SupportedLanguages.Spanish, "es" },
            { SupportedLanguages.Romanian, "ro" },
            { SupportedLanguages.Russian, "ru" },
            { SupportedLanguages.Dutch, "nl" },
            { SupportedLanguages.Slovakian, "sk" },
            { SupportedLanguages.Turkish, "tr" },
            { SupportedLanguages.Ukrainian, "uk" },
            { SupportedLanguages.Serbian, "sr-Cyrl" },
            { SupportedLanguages.Croatian, "hr" },
            { SupportedLanguages.Thai, "th" },
            { SupportedLanguages.Vietnamese, "vi" },
            { SupportedLanguages.BahasaIndonesia, "id" },
            { SupportedLanguages.Hindi, "hi" },
            { SupportedLanguages.Filipino, "fil" },
        };

        public static async Task Run(Options options, State state)
        {
            if (string.IsNullOrWhiteSpace(options.CsAPIKey))
            {
                Trace.TraceError($"Translation API key not specified. Not translating.");
                return;
            }

            foreach (string languageCode in state.LabelsToBeTranslatedPerLangauge.Keys)
            {
                await TranslateAllFor(options, state, languageCode, state.LabelsToBeTranslatedPerLangauge[languageCode]);
            }
            Trace.TraceInformation($"Machine translation done.");
        }

        private static async Task TranslateAllFor(Options options, State state, string languageCode, List<Label> labels)
        {
            Trace.TraceInformation($"Translating {labels.Count} label(s) for {languageCode}...");
            foreach (Label label in labels)
            {
                string mockHtml = $"<body>{label.Source}</body>";
                string translatedMockHtml = Translate(options, mockHtml, options.SourceLanguage, languageCode);

                Regex regex = new Regex("<body>(.*)</body>");
                var v = regex.Match(translatedMockHtml);
                label.Target = v.Groups[1].ToString();
            }

            await Task.FromResult(0);
        }

        private static string securityToken;

        public static string Translate(Options options, string source, string sourceLanguage, string targetLanguage)
        {
            try
            {
                if (null == source)
                {
                    return null;
                }

                securityToken = GetSecurityToken(options);

                if (string.Equals(sourceLanguage, targetLanguage))
                {
                    return source;
                }

                string targetLanguageCode = GetLanguage(targetLanguage);
                string sourceLanguageCode = GetLanguage(sourceLanguage);

                if (string.Equals(sourceLanguageCode, targetLanguageCode))
                {
                    return source;
                }

                XmlDocument document = new XmlDocument();
                document.LoadXml(XmlTransformationUtils.EscapeXmlEntities(source));
                return RecursiveTranslate(options, document, targetLanguage, targetLanguageCode, sourceLanguageCode);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Could not translate html: {ex}");
                return source;
            }
        }

        private static string GetLanguage(string language)
        {
            KeyValuePair<string, string>? result = _languageTable.Where(entry => string.Equals(entry.Key, language, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return result?.Value;
        }

        private static MemoryCache memoryCache = MemoryCache.Default;

        private static string GetSecurityToken(Options options)
        {
            string subscriptionKey = options.CsAPIKey;
            string token = memoryCache.Get("translationToken") as string;
            if (!string.IsNullOrWhiteSpace(token)) return token;

            RestClient client = new RestClient("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

            client.AddDefaultHeader("Ocp-Apim-Subscription-Key", subscriptionKey);
            RestRequest request = new RestRequest();
            IRestResponse response = client.Post(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                // Cache the returned token for a couple of minutes.
                memoryCache.Add("translationToken", response.Content, DateTimeOffset.UtcNow + TimeSpan.FromMinutes(8));
                return response.Content;
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException($"Unauthorized. For an account in the free-tier, this indicates that the account quota has been exceeded.");
            }
            throw new InvalidOperationException($"Unauthorized. Ensure that the key provided is valid");
        }

        private static string RecursiveTranslate(Options options, XmlNode node, string targetLanguage, string language, string sourceLanguageCode)
        {
            if (node.NodeType == XmlNodeType.Text)
            {
                return RecursiveTranslateText(options, targetLanguage, language, sourceLanguageCode, node.OuterXml);
            }
            else if (node.NodeType == XmlNodeType.Element || node.NodeType == XmlNodeType.Document)
            {
                if (node.OuterXml.Length > 5000)
                {
                    string translatedNodeText = node.OuterXml;
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        string translatedChildNodeText = RecursiveTranslate(options, childNode, targetLanguage, language, sourceLanguageCode);
                        translatedNodeText = translatedNodeText.Replace(childNode.OuterXml, translatedChildNodeText);
                    }
                    return translatedNodeText;
                }
                return RetrieveTranslation(options, targetLanguage, language, sourceLanguageCode, node.OuterXml); ;
            }
            else
            {
                return node.OuterXml;
            }
        }

        private static string RecursiveTranslateText(Options options, string targetLanguage, string language, string sourceLanguageCode, string content)
        {
            if (content.Length > 5000)
            {
                int splitindex = content.IndexOf(".", (int)(content.Length / 2));
                if (splitindex == -1)
                {
                    //content cant be split further and is still too big to translate
                    return content;
                }

                string leftresult = RecursiveTranslateText(options, targetLanguage, language, sourceLanguageCode, content.Substring(0, splitindex));
                string rightresult = RecursiveTranslateText(options, targetLanguage, language, sourceLanguageCode, content.Substring(splitindex));

                return leftresult + rightresult;
            }
            return RetrieveTranslation(options, targetLanguage, language, sourceLanguageCode, content, "plain");
        }


        private static string RetrieveTranslation(Options options, string targetLanguage, string language, string sourceLanguageCode, string content, string contentType = "html")
        {
            RestClient client = CreateTranslatorAPIrestClient();

            RestRequest request = new RestRequest("translate", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddQueryParameter("api-version", "3.0");
            request.AddQueryParameter("to", language);
            request.AddQueryParameter("from", sourceLanguageCode);
            request.AddQueryParameter("textType", contentType);

            request.AddJsonBody(new[] { new TranslationRequestBody { Text = content } });

            IRestResponse response = client.Post(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // TODO: Error
                throw new InvalidOperationException($"Request failed with code {response.StatusCode} and message {response.ErrorMessage} ({response.Content})");
            }

            List<TranslationResponseBody> translatedResults = JsonConvert.DeserializeObject<List<TranslationResponseBody>>(response.Content);
            string translatedResult = GetTranslatedTextFromJson(translatedResults, targetLanguage, content);
            return translatedResult;
        }

        private static string GetTranslatedTextFromJson(List<TranslationResponseBody> translationResults, string targetLanguage, string originalText)
        {
            string targetLanguageCode = _languageTable.First(x => x.Key == targetLanguage).Value;

            if (!translationResults.Any(x => x.translations.Any(y => string.Equals(y.to, targetLanguageCode, StringComparison.InvariantCultureIgnoreCase))))
            {
                return originalText;
            }

            return translationResults.First().translations.First(x => string.Equals(x.to, targetLanguageCode, StringComparison.InvariantCultureIgnoreCase)).text;
        }


        private static RestClient CreateTranslatorAPIrestClient()
        {
            RestClient client = new RestClient("https://api.cognitive.microsofttranslator.com/");
            client.AddDefaultHeader("Authorization", $"Bearer {securityToken}");
            return client;
        }

        public class TranslationRequestBody
        {
            /// <summary>
            /// Text to be translated
            /// </summary>
            public string Text { get; set; }
        }

        /// <summary>
        /// The response body sent from azure translate api (v3.0) deserialised from JSON
        /// </summary>
        public class TranslationResponseBody
        {
            /// <summary>
            /// A list of translations for each request sent to azure api (v3.0)
            /// </summary>
            public List<TranslationResponseBodyDetail> translations { get; set; }
        }

        /// <summary>
        /// The detail of a translation response body
        /// </summary>
        public class TranslationResponseBodyDetail
        {
            /// <summary>
            /// Translated text
            /// </summary>
            public string text { get; set; }
            /// <summary>
            /// The language code that the text was translated to
            /// </summary>
            public string to { get; set; }
        }

        public static class SupportedLanguages
        {
            /// <summary>The default publication language, whatever it may be</summary>
            public const string Default = "default";
            /// <summary>Langauage code definition</summary>
            public const string Bulgarian = "bg-BG";
            /// <summary>Langauage code definition</summary>
            public const string Danish = "da-DK";
            /// <summary>Langauage code definition</summary>
            public const string Dutch = "nl-NL";
            /// <summary>Langauage code definition</summary>
            public const string English = "en-US";
            /// <summary>Langauage code definition</summary>
            public const string French = "fr-FR";
            /// <summary>Langauage code definition</summary>
            public const string German = "de-DE";
            /// <summary>Langauage code definition</summary>
            public const string Hungarian = "hu-HU";
            /// <summary>Langauage code definition</summary>
            public const string Italian = "it-IT";
            /// <summary>Langauage code definition</summary>
            public const string Japanese = "ja-JP";
            /// <summary>Langauage code definition</summary>
            public const string Polish = "pl-PL";
            /// <summary>Langauage code definition</summary>
            public const string Portuguese = "pt-PT";
            /// <summary>Langauage code definition</summary>
            public const string Romanian = "ro-RO";
            /// <summary>Langauage code definition</summary>
            public const string SimplifiedChinese = "zh-CHS";
            /// <summary>Langauage code definition</summary>
            public const string Slovakian = "sk-SK";
            /// <summary>Langauage code definition</summary>
            public const string Spanish = "es-ES";
            /// <summary>Langauage code definition</summary>
            public const string Russian = "ru-RU";
            /// <summary>Langauage code definition</summary>
            public const string Turkish = "tr-TR";
            /// <summary>Langauage code definition</summary>
            public const string Ukrainian = "uk-UA";
            /// <summary>Langauage code definition</summary>
            public const string Serbian = "sr-Cyrl";
            /// <summary>Langauage code definition</summary>
            public const string Croatian = "hr-HR";
            /// <summary>Langauage code definition</summary>
            public const string Thai = "th-TH";
            /// <summary>Langauage code definition</summary>
            public const string Vietnamese = "vi-VN";
            /// <summary>Langauage code definition</summary>
            public const string BahasaIndonesia = "id-ID";
            /// <summary>Langauage code definition</summary>
            public const string Hindi = "hi-IN";
            /// <summary>Langauage code definition</summary>
            public const string Filipino = "fil-PH";

            /// <summary>
            /// 
            /// </summary>
            public class LanguageDefinition
            {
                /// <summary>
                /// 
                /// </summary>
                public string LanguageCode { get; set; }

                /// <summary>
                /// 
                /// </summary>
                public string Title { get; set; }
            }

            /// <summary>
            /// All supported languages
            /// </summary>
            public static readonly LanguageDefinition[] All = new LanguageDefinition[]
            {
            new LanguageDefinition { LanguageCode=Bulgarian, Title="Bulgarian" },
            new LanguageDefinition { LanguageCode=Danish, Title="Danish" },
            new LanguageDefinition { LanguageCode=Dutch, Title="Dutch" },
            new LanguageDefinition { LanguageCode=English, Title="English" },
            new LanguageDefinition { LanguageCode=French, Title="French" },
            new LanguageDefinition { LanguageCode=German, Title="German" },
            new LanguageDefinition { LanguageCode=Hungarian, Title="Hungarian" },
            new LanguageDefinition { LanguageCode=Italian, Title="Italian" },
            new LanguageDefinition { LanguageCode=Japanese, Title="Japanese" },
            new LanguageDefinition { LanguageCode=Polish, Title="Polish" },
            new LanguageDefinition { LanguageCode=Portuguese, Title="Portuguese" },
            new LanguageDefinition { LanguageCode=Romanian, Title="Romanian" },
            new LanguageDefinition { LanguageCode=SimplifiedChinese, Title="Simplified Chinese" },
            new LanguageDefinition { LanguageCode=Slovakian, Title="Slovakian" },
            new LanguageDefinition { LanguageCode=Spanish, Title="Spanish" },
            new LanguageDefinition { LanguageCode=Russian, Title="Russian" },
            new LanguageDefinition { LanguageCode=Turkish, Title="Turkish" },
            new LanguageDefinition { LanguageCode=Ukrainian, Title="Ukrainian" },
            new LanguageDefinition { LanguageCode=Serbian, Title="Serbian" },
            new LanguageDefinition { LanguageCode=Croatian, Title="Croatian" },
            new LanguageDefinition { LanguageCode=Thai, Title="Thai" },
            new LanguageDefinition { LanguageCode=Vietnamese, Title="Vietnamese" },
            new LanguageDefinition { LanguageCode=BahasaIndonesia, Title="Bahasa Indonesia" },
            new LanguageDefinition { LanguageCode=Hindi, Title="Hindi" },
            new LanguageDefinition { LanguageCode=Filipino, Title="Filipino" },
            };

            /// <summary>
            /// True if and only if the language code is supported, case does play a role here
            /// </summary>
            /// <param name="languageCode"></param>
            /// <returns></returns>
            public static bool IsSupportedLanguage(string languageCode)
            {
                return All.Any(x => string.Equals(x.LanguageCode, languageCode));
            }

            /// <summary>
            /// All supported language codes.
            /// </summary>
            public static string[] GetCodes()
            {
                return All.Select(x => x.LanguageCode).ToArray();
            }
        }
    }
}
