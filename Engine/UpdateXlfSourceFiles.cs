using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace label_translator.Engine
{
    public class UpdateXlfSourceFiles
    {
        public static async Task Run(Options options, State state)
        {
            string backupFolder = null;

            foreach (string language in state.DataPerLanguage.Keys)
            {
                var languageData = state.DataPerLanguage[language];

                if (string.IsNullOrWhiteSpace(backupFolder))
                {
                    backupFolder = Path.Combine(Path.GetDirectoryName(languageData.XlifFile.FullName), $"BACKUP-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}");
                    Directory.CreateDirectory(backupFolder);
                }

                // Let's back up the original.
                File.Copy(languageData.XlifFile.FullName, Path.Combine(backupFolder, languageData.XlifFile.Name));

                OverwriteMachineTranslatedLabels(state, language, languageData);
                OverwriteLAbelsWithExcelOverrides(state, language, languageData);

                languageData.XmlDocument.Save(languageData.XlifFile.FullName);
            }

            await Task.FromResult(0);
        }

        private static void OverwriteMachineTranslatedLabels(State state, string language, LanguageData languageData)
        {
            foreach (var translatedLabel in state.LabelsToBeTranslatedPerLangauge[language])
            {
                UpdateLabelInXLF(language, languageData, translatedLabel);
            }
        }


        private static void OverwriteLAbelsWithExcelOverrides(State state, string language, LanguageData languageData)
        {
            foreach (var labelOverride in state.DataPerLanguage[language].Labels.Values.Where(l => l.HasOverrideInExcelFile))
            {
                UpdateLabelInXLF(language, languageData, labelOverride);
            }
        }

        private static void UpdateLabelInXLF(string language, LanguageData languageData, Label translation)
        {
            XmlElement labelElement = languageData.XmlDocument.SelectSingleNode($"//doc:trans-unit[@id='{translation.ID}']", languageData.NamespaceManager) as XmlElement;
            if (null != labelElement)
            {
                try
                {
                    var targetElement = labelElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == "target");
                    targetElement.InnerXml = translation.Target;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to set '{translation.Target}' as InnerXML for label {translation.ID} in language {language}: {ex.GetType().Name}; {ex.Message}");
                    // Trace.TraceInformation($"{ex}");
                }
            }
            else
            {
                Trace.TraceError($"I have a translation for label {translation.ID} in language {language} but I can't find the XML element anymore.");
            }
        }

    }
}
