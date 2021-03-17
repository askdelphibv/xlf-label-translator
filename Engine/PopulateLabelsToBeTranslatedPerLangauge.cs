using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                AddLabelsWithChangedSourceTexts(options, state, language);
                AddNewOrUntranslatedLabels(options, state, language);
            }

            RemoveDuplicatesFromLabelsToBeTranslatedPerLanguage(options, state);

            await Task.FromResult(0);
        }

        private static void AddLabelsWithChangedSourceTexts(Options options, State state, string language)
        {
            // Copy the source text from the default language
            LanguageData data = state.DataPerLanguage[language];
            foreach (Label label in data.Labels.Values)
            {
                if (state.SourceLabels.ContainsKey(label.ID))
                {
                    // The source text has changed, we should translate the label again and update the source
                    if (!AreTheSame(label.Source, state.SourceLabels[label.ID].Source))
                    {
                        label.Source = state.SourceLabels[label.ID].Source;

                        state.LabelsToBeTranslatedPerLanguage[language].Add(new Label
                        {
                            ID = label.ID,
                            Source = state.SourceLabels[label.ID].Source,
                            Target = string.Empty
                        });
                    }
                }
            }
        }

        private static void AddNewOrUntranslatedLabels(Options options, State state, string language)
        {
            LanguageData data = state.DataPerLanguage[language];

            IEnumerable<Label> labelsWithoutTranslation = data.Labels.Select(label => label.Value).Where(label => !label.HasOverrideInExcelFile && (string.IsNullOrWhiteSpace(label.Target) || AreTheSame(label.Source, label.Target)));

            state.LabelsToBeTranslatedPerLanguage[language].AddRange(labelsWithoutTranslation.Select(label => new Label
            {
                ID = label.ID,
                Source = label.Source,
                Target = string.Empty
            }));
        }

        private static void RemoveDuplicatesFromLabelsToBeTranslatedPerLanguage(Options options, State state)
        {
            foreach (var language in state.LabelsToBeTranslatedPerLanguage.Keys)
            {
                state.LabelsToBeTranslatedPerLanguage[language] = state.LabelsToBeTranslatedPerLanguage[language].OrderBy(x => x.ID).Distinct(new LabelIDComparer()).ToList();
            }
        }

        private static bool AreTheSame(string source, string target)
        {
            string normalizedSource = (source?.ToLowerInvariant() ?? "").Trim();
            string normalizedTarget = (target?.ToLowerInvariant() ?? "").Trim();
            return string.Equals(normalizedSource, normalizedTarget, StringComparison.InvariantCultureIgnoreCase);
        }

        private class LabelIDComparer : IEqualityComparer<Label>
        {
            public bool Equals(Label x, Label y)
            {
                return string.Equals(x?.ID, y?.ID);
            }

            public int GetHashCode([DisallowNull] Label obj)
            {
                return (obj?.ID ?? string.Empty).GetHashCode();
            }
        }
    }
}
