using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace label_translator.Engine
{
    public static class PopulateOverrides
    {
        public static async Task Run(Options options, State state)
        {
            DirectoryInfo sourceDirectoryInfo = new DirectoryInfo(options.SourceFolder);

            string basename = Path.GetFileNameWithoutExtension(options.BaseFile);

            FileInfo[] files = sourceDirectoryInfo.GetFiles($"{basename}-overrides*.xlsx");
            foreach (FileInfo file in files.OrderBy(f => f.Name))
            {
                using (var package = new ExcelPackage(new FileInfo(file.FullName)))
                {
                    Trace.TraceInformation($"Processing {file.Name} for overrides.");
                    IEnumerable<ExcelWorksheet> worksheets = package.Workbook.Worksheets.Where(x => x.Hidden == eWorkSheetHidden.Visible); // only process visible worksheets
                    foreach (var worksheet in worksheets)
                    {
                        if (state.DataPerLanguage.ContainsKey(worksheet.Name)) // workshete name must be a language code
                        {
                            await LoadOverridesForLanguage(options, state, file, language: worksheet.Name, worksheet: worksheet);
                        }
                        else
                        {
                            Trace.TraceError($"Error: Overrides file {file.Name} contains worksheet for unsupported language {worksheet.Name}");
                        }
                    }
                }
            }
        }

        private static async Task LoadOverridesForLanguage(Options options, State state, FileInfo file, string language, ExcelWorksheet worksheet)
        {
            int count = 0;
            LanguageData languageData = state.DataPerLanguage[language];
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string id = worksheet.Cells[row, 1].Text;
                // Not needed: string source = worksheet.Cells[row, 2].Text;
                string target = worksheet.Cells[row, 3].Text;

                if (string.IsNullOrWhiteSpace(id)) continue; // ignore empty rows

                if (languageData.Labels.ContainsKey(id))
                {
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        Label label = languageData.Labels[id];
                        // Work around issue with old versions of this code that would erroneously insert <body> in the target labels.
                        if (Regex.IsMatch(target, @"<body>(.*)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                        {
                            target = Regex.Replace(target, @"<body>(.*)</body>", (me) => $"{me.Groups[1].Value}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        }
                        label.Target = target;
                        label.HasOverrideInExcelFile = true;
                        count++;
                    }
                    else
                    {
                        Trace.TraceWarning($"Warning: {file.Name}: Label {id} has empty target for language {language}. Empty overrides should be removed.");
                    }
                }
                else
                {
                    Trace.TraceWarning($"Warning: {file.Name}: Label {id} does not exist in source file(s) for language {language}");
                }
            }

            Trace.TraceInformation($"... added {count} override(s) for {language}");

            await Task.FromResult(0);
        }
    }
}
