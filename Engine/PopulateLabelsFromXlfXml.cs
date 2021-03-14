using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace label_translator.Engine
{
    public static class PopulateLabelsFromXlfXml
    {
        public static async Task Run(Options options, State state)
        {
            foreach (string language in state.DataPerLanguage.Keys)
            {
                System.Diagnostics.Trace.TraceInformation($"Loading labels for {language} from {state.DataPerLanguage[language].XlifFile.Name}");
                LoadLabelsForLanguage(options, state, language, state.DataPerLanguage[language]);
            }

            await Task.FromResult(0);
        }

        private static void LoadLabelsForLanguage(Options options, State state, string language, LanguageData languageData)
        {
            bool isDefaultLanguage = string.Equals(language, options.SourceLanguage, StringComparison.InvariantCultureIgnoreCase);

            XmlElement[] labelElements = languageData.XmlDocument.SelectNodes($"//doc:trans-unit", languageData.NamespaceManager).OfType<XmlElement>().ToArray();

            foreach (XmlElement labelElement in labelElements)
            {
                Label label = new Label();
                label.ID = labelElement.GetAttribute("id");

                var sourceElement = labelElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == "source");
                label.Source = sourceElement?.InnerXml.Replace($" xmlns=\"{languageData.XmlDocument.DocumentElement.NamespaceURI}\"", "");

                var targetElement = labelElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == "target");
                label.Target = targetElement?.InnerXml.Replace($" xmlns=\"{languageData.XmlDocument.DocumentElement.NamespaceURI}\"", "");

                if (!string.IsNullOrWhiteSpace(label.Source) && !string.IsNullOrWhiteSpace(label.ID))
                {
                    languageData.Labels[label.ID] = label;

                    AddLabelToAllOtherLanguages(state, language, isDefaultLanguage, label);
                }
            }

            System.Diagnostics.Trace.TraceInformation($"Loaded {languageData.Labels.Count()} label(s) from XML.");
        }

        private static void AddLabelToAllOtherLanguages(State state, string language, bool isDefaultLanguage, Label label)
        {
            if (isDefaultLanguage)
            {
                foreach (var languageKVP in state.DataPerLanguage.Where(x => x.Key != language))
                {
                    if (!languageKVP.Value.Labels.ContainsKey(label.ID))
                    {
                        languageKVP.Value.Labels[label.ID] = new Label
                        {
                            ID = label.ID,
                            Source = label.Source,
                            Target = string.Empty
                        };
                    }
                }
            }
        }
    }
}