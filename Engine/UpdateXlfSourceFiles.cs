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

                // Apply automatic translations and then the manual translations
                OverwriteMachineTranslatedLabels(state, language, languageData);
                OverwriteLAbelsWithExcelOverrides(state, language, languageData);
                
                // If requested, fix the source elements in the translation files
                if (options.FixSource && !string.Equals(language, options.SourceLanguage, StringComparison.InvariantCultureIgnoreCase))
                {
                    FixSourceElements(state, language, languageData);
                }

                languageData.XmlDocument.Save(languageData.XlifFile.FullName);
            }

            await Task.FromResult(0);
        }

        private static void OverwriteMachineTranslatedLabels(State state, string language, LanguageData languageData)
        {
            foreach (var translatedLabel in state.LabelsToBeTranslatedPerLanguage[language])
            {
                UpdateLabelInXlf(language, languageData, translatedLabel);
            }
        }


        private static void OverwriteLAbelsWithExcelOverrides(State state, string language, LanguageData languageData)
        {
            foreach (var labelOverride in state.DataPerLanguage[language].Labels.Values.Where(l => l.HasOverrideInExcelFile))
            {
                UpdateLabelInXlf(language, languageData, labelOverride);
            }
        }

        private static void FixSourceElements(State state, string language, LanguageData languageData)
        {
            foreach (var translatedLabel in state.DataPerLanguage[language].Labels?.Values)
            {
                OverwriteLabelSource(language, languageData, translatedLabel);
            }
        }

        private static void UpdateLabelInXlf(string language, LanguageData languageData, Label translation)
        {
            XmlElement labelElement = languageData.XmlDocument.SelectSingleNode($"//doc:trans-unit[@id='{translation.ID}']", languageData.NamespaceManager) as XmlElement;
            if (null != labelElement)
            {
                try
                {
                    var targetElement = labelElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == "target");
                    if (null == targetElement)
                    {
                        targetElement = languageData.XmlDocument.CreateElement("target");
                        labelElement.AppendChild(targetElement);
                    }
                    targetElement.InnerXml = translation.Target;
                    targetElement.SetAttribute("state", "final");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to set '{translation.Target}' as InnerXML for label {translation.ID} in language {language}: {ex.GetType().Name}; {ex.Message}");
                    // Trace.TraceInformation($"{ex}");
                }
            }
            else
            {
                Trace.TraceInformation($"I have a translation for label {translation.ID} in language {language} but I can't find the XML element. Adding missing label.");

                AddAdditionalLabelToXlfDocument(languageData, translation);
            }
        }

        private static void OverwriteLabelSource(string language, LanguageData languageData, Label translation)
        {

            XmlElement labelElement = languageData.XmlDocument.SelectSingleNode($"//doc:trans-unit[@id='{translation.ID}']", languageData.NamespaceManager) as XmlElement;
            if (null != labelElement)
            {
                try
                {
                    var targetElement = labelElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == "source");
                    if (null == targetElement)
                    {
                        targetElement = languageData.XmlDocument.CreateElement("source");
                        labelElement.AppendChild(targetElement);
                    }
                    targetElement.InnerXml = translation.Source;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to set source '{translation.Source}' as InnerXML for label {translation.ID} in language {language}: {ex.GetType().Name}; {ex.Message}");
                    // Trace.TraceInformation($"{ex}");
                }
            }
            else
            {
                Trace.TraceInformation($"I have a translation for label {translation.ID} in language {language} but I can't find the XML element. Adding missing label.");

                AddAdditionalLabelToXlfDocument(languageData, translation);
            }
        }

        private static void AddAdditionalLabelToXlfDocument(LanguageData languageData, Label translation)
        {
            XmlElement transUnitElt = languageData.XmlDocument.CreateElement("trans-unit");
            transUnitElt.SetAttribute("id", translation.ID);

            XmlElement sourceElement = languageData.XmlDocument.CreateElement("source");
            sourceElement.InnerXml = translation.Source;
            transUnitElt.AppendChild(sourceElement);

            XmlElement targetElement = languageData.XmlDocument.CreateElement("target");
            targetElement.InnerXml = translation.Target;
            targetElement.SetAttribute("state", "final");
            transUnitElt.AppendChild(targetElement);


            XmlElement bodyElement = languageData.XmlDocument.SelectSingleNode($"//doc:body", languageData.NamespaceManager) as XmlElement;
            bodyElement.AppendChild(transUnitElt);
        }
    }
}
