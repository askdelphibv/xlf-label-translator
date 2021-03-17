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
        public static async Task Run(Options options, State state)
        {
            if (string.IsNullOrWhiteSpace(options.CsAPIKey))
            {
                Trace.TraceError($"Translation API key not specified. Not translating.");
                return;
            }

            foreach (string languageCode in state.LabelsToBeTranslatedPerLangauge.Keys)
            {
                if (!string.IsNullOrWhiteSpace(GetAzureTargetLanguageCodeFor(languageCode)))
                {
                    await TranslateAllFor(options, state, languageCode, state.LabelsToBeTranslatedPerLangauge[languageCode]);
                }
                else
                {
                    Trace.TraceError($"Can't find target language for {languageCode}");
                }
            }
            Trace.TraceInformation($"Machine translation done.");
        }

        private static async Task TranslateAllFor(Options options, State state, string languageCode, List<Label> labels)
        {
            Trace.TraceInformation($"Translating {labels.Count} label(s) for {languageCode}...");
            foreach (Label label in labels)
            {
                string sourceLabel = CalculateSourceLabelForTranslation(options, state, label);
                sourceLabel = Regex.Replace(sourceLabel, "[#]([0-9])[#]", (me) => $"<span class=\"ESCAPED_{me.Groups[1].Value}\">{me.Groups[1].Value}</span>");
                string mockHtml = $"<body>{sourceLabel}</body>";

                string translationResult = Translate(options, mockHtml, options.SourceLanguage, languageCode);

                translationResult = Regex.Replace(translationResult, @"<span\s*class=[""]\s*ESCAPED_([0-9])\s*[""]\s*>[^<]*</span>", (me) => $"#{me.Groups[1].Value}#", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                translationResult = Regex.Replace(translationResult, @"<body>(.*)</body>$", (me) => $"{me.Groups[1].Value}", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                label.Target = translationResult;
            }

            await Task.FromResult(0);
        }

        private static string CalculateSourceLabelForTranslation(Options options, State state, Label label)
        {
            // Prefer the source from the original language file. Only when that's not present use the source from the XLF file.
            string sourceLabel = state.DataPerLanguage[options.SourceLanguage].Labels.FirstOrDefault(x => x.Key == label.ID).Value?.Source;
            if (string.IsNullOrWhiteSpace(sourceLabel))
            {
                sourceLabel = label.Source;
            }

            return sourceLabel;
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

                string targetLanguageCode = GetAzureTargetLanguageCodeFor(targetLanguage);
                string sourceLanguageCode = GetAzureTargetLanguageCodeFor(sourceLanguage);

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

        private static string GetAzureTargetLanguageCodeFor(string language)
        {
            KeyValuePair<string, string>? result = SupportedLanguages.LanguageTable.Where(entry => string.Equals(entry.Key, language, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
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
            string targetLanguageCode = GetAzureTargetLanguageCodeFor(targetLanguage);

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
    }
}
