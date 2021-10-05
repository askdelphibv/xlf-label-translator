using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace label_translator
{
    public static class SupportedLanguages
    {
        public const string Default = "default";
        public const string Bulgarian = "bg-BG";
        public const string Danish = "da-DK";
        public const string Dutch = "nl-NL";
        public const string English = "en-US";
        public const string French = "fr-FR";
        public const string German = "de-DE";
        public const string Hungarian = "hu-HU";
        public const string Italian = "it-IT";
        public const string Japanese = "ja-JP";
        public const string Polish = "pl-PL";
        public const string Portuguese = "pt-PT";
        public const string Romanian = "ro-RO";
        public const string SimplifiedChinese = "zh-CHS";
        public const string Slovakian = "sk-SK";
        public const string Spanish = "es-ES";
        public const string Russian = "ru-RU";
        public const string Turkish = "tr-TR";
        public const string Ukrainian = "uk-UA";
        public const string Serbian = "sr-Cyrl";
        public const string Croatian = "hr-HR";
        public const string Thai = "th-TH";
        public const string Vietnamese = "vi-VN";
        public const string BahasaIndonesia = "id-ID";
        public const string Hindi = "hi-IN";
        public const string Filipino = "fil-PH";
        public const string Urdu = "ur-PK";
        public const string Bengali = "bn-BD";
        public const string Korean = "ko-KR";

        public static readonly Dictionary<string, string> LanguageTable = new Dictionary<string, string>
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
            { SupportedLanguages.Urdu, "ur" },
            { SupportedLanguages.Bengali, "bn" },
            { SupportedLanguages.Korean, "ko" },
        };
    }
}
