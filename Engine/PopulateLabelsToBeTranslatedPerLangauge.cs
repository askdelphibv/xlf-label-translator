using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace label_translator.Engine
{
    public static class PopulateLabelsToBeTranslatedPerLangauge
    {
        public static async Task Run(Options options, State state)
        {
            foreach (string language in state.DataPerLanguage.Keys)
            {
                await PopulateLabelsToBeTranslatedFor(options, state, language);
            }

            await Task.FromResult(0);
        }

        private static async Task PopulateLabelsToBeTranslatedFor(Options options, State state, string language)
        {
            LanguageData data = state.DataPerLanguage[language];

            IEnumerable<Label> labelsWithoutTranslation = data.Labels.Select(label => label.Value).Where(label => !label.HasOverrideInExcelFile && (string.IsNullOrWhiteSpace(label.Target) || AreTheSame(label.Source, label.Target)));

            state.LabelsToBeTranslatedPerLangauge[language].AddRange(labelsWithoutTranslation.Select(label => new Label { 
                HasOverrideInExcelFile = label.HasOverrideInExcelFile,
                ID = label.ID,
                Source = label.Source,
                Target = string.Empty
            }));

            await Task.FromResult(0);
        }

        private static bool AreTheSame(string source, string target)
        {
            string normalizedSource = (source?.ToLowerInvariant() ?? "").Trim();
            string normalizedTarget = (target?.ToLowerInvariant() ?? "").Trim();
            return string.Equals(normalizedSource, normalizedTarget, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
