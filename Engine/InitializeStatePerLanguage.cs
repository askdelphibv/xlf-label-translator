using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace label_translator.Engine
{
    public static class InitializeStatePerLanguage
    {
        // IN: options.Folder and options.BaseName
        // OUT: state.DataPerLanguage created for each language, with initialized Filename and XmlDocument
        //      state.LabelsToBeTranslatedPerLangauge created for each language
        public static async Task Run(Options options, State state)
        {
            DirectoryInfo sourceDirectoryInfo = new DirectoryInfo(options.SourceFolder);

            string basename = Path.GetFileNameWithoutExtension(options.BaseFile);

            FileInfo[] files = sourceDirectoryInfo.GetFiles($"{basename}*.xlf");
            foreach (FileInfo file in files)
            {
                string language = ExtractLanguageComponentFromFilename(basename, file);
                if (!string.IsNullOrWhiteSpace(language))
                {
                    LanguageData languageData = new LanguageData();
                    languageData.XlifFile = file;
                    languageData.XmlDocument = new XmlDocument();
                    languageData.XmlDocument.Load(file.FullName);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(languageData.XmlDocument.NameTable);
                    nsmgr.AddNamespace("doc", languageData.XmlDocument.DocumentElement.NamespaceURI);
                    languageData.NamespaceManager = nsmgr;

                    state.DataPerLanguage[language] = languageData;

                    state.LabelsToBeTranslatedPerLangauge[language] = new List<Label>();
                }
            }

            await Task.FromResult(0);
        }

        private static string ExtractLanguageComponentFromFilename(string basename, FileInfo file)
        {
            // extract language from filename
            Match match = Regex.Match(file.Name, $"^{Regex.Escape(basename)}[.-](.*)[.]xl[a-z]*f$", RegexOptions.IgnoreCase);
            string language = match.Success ? match.Groups[1].Value : null;
            return language;
        }
    }
}
