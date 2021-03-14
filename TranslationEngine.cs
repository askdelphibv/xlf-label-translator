using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace label_translator
{
    public class TranslationEngine
    {
        private readonly Options options;

        public TranslationEngine(Options options)
        {
            this.options = options;
        }

        // Create LanguageData files for all XLF files, except the source lamguage

        // Load all source and target labels from all XLF files and build a dictionary of dictionaries

        // Load all overrides from the Excel file(s) named "message-overrides-*.xlsx"

        // For each label,language combination for which
        // - There is no override yet AND there is only a default (identical to source) or empty translation
        // - Create an empty entry for the (label, language, source, target) tuple in a "labels-to-be-translated" collection

        // For each entry in "labels-to-be-translated"
        // - Machine translate the label and write the translated value to "target" in the "labels-to-be-translated" collection
        // - For each machine-translated label, overwrite also the "XLF" file contents in-memory for the target element

        // Write the labels-to-be-translated to "message-overrides-<yymmddhhMMss>.xlsx"

        // Update all "messages.<language>.xlf" with the updated in-memory labels
        internal async Task Run()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            State state = new State();
            await Engine.InitializeStatePerLanguage.Run(options, state);
            await Engine.PopulateLabelsFromXlfXml.Run(options, state);
            await Engine.PopulateOverrides.Run(options, state);
            await Engine.PopulateLabelsToBeTranslatedPerLangauge.Run(options, state);
            await Engine.MachineTranslateMissingLabels.Run(options, state);
            await Engine.WriteNewTranslationExcelFile.Run(options, state);
            await Engine.UpdateXlfSourceFiles.Run(options, state);
        }
    }
}

