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
                    InitializeDataBlocksFromFileInfo(state, file, language);
                }
            }

            if (!state.DataPerLanguage.ContainsKey(options.SourceLanguage))
            {
                FileInfo file = new FileInfo(Path.Combine(sourceDirectoryInfo.FullName, $"{basename}.{options.SourceLanguage}.xlf"));
                InitializeDataBlocksFromFileInfo(state, file, options.SourceLanguage);
            }

            state.SourceLabels = state.DataPerLanguage[options.SourceLanguage].Labels;

            await Task.FromResult(0);
        }

        private static void InitializeDataBlocksFromFileInfo(State state, FileInfo file, string language)
        {
            LanguageData languageData = new LanguageData();
            languageData.XlifFile = file;
            languageData.XmlDocument = new XmlDocument();
            if (file.Exists)
            {
                languageData.XmlDocument.Load(file.FullName);
            }
            else
            {
                languageData.XmlDocument.LoadXml($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<xliff version=\"1.2\"><file datatype=\"plaintext\" source-language=\"{language}\"><body></body></file></xliff>");
            }
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(languageData.XmlDocument.NameTable);
            nsmgr.AddNamespace("doc", languageData.XmlDocument.DocumentElement.NamespaceURI);
            languageData.NamespaceManager = nsmgr;

            state.DataPerLanguage[language] = languageData;

            state.LabelsToBeTranslatedPerLanguage[language] = new List<Label>();
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
