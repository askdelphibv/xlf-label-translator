using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace label_translator.Engine
{
    public static class WriteNewTranslationExcelFile
    {
        public static async Task Run(Options options, State state)
        {
            string basename = Path.GetFileNameWithoutExtension(options.BaseFile);

            FileInfo fi = new FileInfo(Path.Combine(options.SourceFolder, $"{basename}-overrides-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.xlsx"));

            using (ExcelPackage package = new ExcelPackage(fi))
            {
                foreach (var language in state.LabelsToBeTranslatedPerLanguage.Keys)
                {
                    await AddWorksheet(options, state, language, state.LabelsToBeTranslatedPerLanguage[language], package);
                }

                await package.SaveAsync();
            }
        }

        private static async Task AddWorksheet(Options options, State state, string language, List<Label> labels, ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add(language);
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Source";
            worksheet.Cells[1, 3].Value = "Target";

            worksheet.Cells["A1:C1"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
            worksheet.Cells["A1:C1"].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
            worksheet.Cells["A1:C1"].Style.Font.Bold = true;
            worksheet.Cells["A1:C1"].Style.Fill.SetBackground(System.Drawing.Color.LightGray);

            worksheet.Column(1).AutoFit();
            worksheet.Column(2).Width = 100;
            worksheet.Column(3).Width = 100;

            int row = 2;
            foreach (Label label in labels ?? new List<Label>())
            {
                worksheet.Cells[row, 1].Value = label.ID;
                worksheet.Cells[row, 2].Value = label.Source;
                worksheet.Cells[row, 3].Value = label.Target;
                row++;
            }

            await Task.FromResult(0);
        }
    }
}
