using DocumentFormat.OpenXml.Packaging;
using ICSharpCode.SharpZipLib.Zip;
using OfficeOpenXml;
using sara_coursework.data;
using sara_coursework.models;
using sara_coursework.Services.Security;
using sara_coursework.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace sara_coursework.Services
{
    public interface IExportService
    {
        void ExportToExcel(string filePath, List<string> selectedSheets, List<AwardingViewModel> awardings);
        void ExportToWord(string filePath, string awardsText, string periodText, List<AwardingViewModel> awardings, Action<int, string>? updateProgress = null);
        void CreateZipArchive(string excelPath, string wordPath, string zipPath, bool hasExcel, bool hasWord, string? password = null);
    }

    public class ExportService : IExportService
    {
        public void ExportToExcel(string filePath, List<string> selectedSheets, List<AwardingViewModel> awardings)
        {
            using var context = new AppDbContext();
            using var package = new ExcelPackage();

            if (selectedSheets.Contains("Награждения"))
            {
                var ws = package.Workbook.Worksheets.Add("Награждения");
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "ФИО/Коллектив";
                ws.Cells[1, 3].Value = "Тип";
                ws.Cells[1, 4].Value = "Должность";
                ws.Cells[1, 5].Value = "Награда";
                ws.Cells[1, 6].Value = "Основание";
                ws.Cells[1, 7].Value = "Номер приказа";
                ws.Cells[1, 8].Value = "Дата";

                int row = 2;
                foreach (var item in awardings)
                {
                    ws.Cells[row, 1].Value = item.Id;
                    ws.Cells[row, 2].Value = item.AwardedName;
                    ws.Cells[row, 3].Value = item.AwardedType;
                    ws.Cells[row, 4].Value = item.Position;
                    ws.Cells[row, 5].Value = item.AwardTitle;
                    ws.Cells[row, 6].Value = item.Reason;
                    ws.Cells[row, 7].Value = item.DecreeNumber;
                    ws.Cells[row, 8].Value = item.DecreeDate.ToString("dd.MM.yyyy");
                    row++;
                }
            }

            if (selectedSheets.Contains("Награждаемые"))
            {
                var ws = package.Workbook.Worksheets.Add("Награждаемые");
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "Тип";
                ws.Cells[1, 3].Value = "Название/ФИО";
                ws.Cells[1, 4].Value = "Фамилия";
                ws.Cells[1, 5].Value = "Имя";
                ws.Cells[1, 6].Value = "Отчество";
                ws.Cells[1, 7].Value = "Должность";
                ws.Cells[1, 8].Value = "Коллектив ID";

                var awardedList = context.Awarded.ToList();
                int row = 2;
                foreach (var item in awardedList)
                {
                    ws.Cells[row, 1].Value = item.Id;
                    if (item is Citizen citizen)
                    {
                        ws.Cells[row, 2].Value = "Гражданин";
                        ws.Cells[row, 3].Value = citizen.ToString();
                        ws.Cells[row, 4].Value = citizen.LastName;
                        ws.Cells[row, 5].Value = citizen.FirstName;
                        ws.Cells[row, 6].Value = citizen.MiddleName;
                        ws.Cells[row, 7].Value = citizen.Position;
                        ws.Cells[row, 8].Value = citizen.CollectiveId;
                    }
                    else if (item is Collective collective)
                    {
                        ws.Cells[row, 2].Value = "Коллектив";
                        ws.Cells[row, 3].Value = collective.CollectiveName;
                    }
                    row++;
                }
            }

            if (selectedSheets.Contains("Постановления"))
            {
                var ws = package.Workbook.Worksheets.Add("Постановления");
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "Номер";
                ws.Cells[1, 3].Value = "Дата";
                ws.Cells[1, 4].Value = "Основание ID";

                var decrees = context.Decrees.ToList();
                int row = 2;
                foreach (var item in decrees)
                {
                    ws.Cells[row, 1].Value = item.Id;
                    ws.Cells[row, 2].Value = item.Number;
                    ws.Cells[row, 3].Value = item.Date.ToString("dd.MM.yyyy");
                    ws.Cells[row, 4].Value = item.AwardReasonId;
                    row++;
                }
            }

            if (selectedSheets.Contains("Награды"))
            {
                var ws = package.Workbook.Worksheets.Add("Награды");
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "Название";

                var awards = context.Awards.ToList();
                int row = 2;
                foreach (var item in awards)
                {
                    ws.Cells[row, 1].Value = item.Id;
                    ws.Cells[row, 2].Value = item.AwardName;
                    row++;
                }
            }

            if (selectedSheets.Contains("Основания"))
            {
                var ws = package.Workbook.Worksheets.Add("Основания");
                ws.Cells[1, 1].Value = "ID";
                ws.Cells[1, 2].Value = "Наименование";

                var reasons = context.AwardReasons.ToList();
                int row = 2;
                foreach (var item in reasons)
                {
                    ws.Cells[row, 1].Value = item.Id;
                    ws.Cells[row, 2].Value = item.ReasonName;
                    row++;
                }
            }

            package.SaveAs(new FileInfo(filePath));
        }

        public void ExportToWord(string filePath, string awardsText, string periodText, List<AwardingViewModel> awardings, Action<int, string>? updateProgress = null)
        {
            updateProgress?.Invoke(10, "Поиск шаблона...");

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "template", "template.docx");

            if (!File.Exists(templatePath))
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                templatePath = Path.Combine(assemblyPath, "resources", "template", "template.docx");
            }

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Файл шаблона не найден по пути: {templatePath}");
            }

            updateProgress?.Invoke(30, "Подготовка временного файла...");
            string tempPath = Path.GetTempFileName();
            File.Copy(templatePath, tempPath, overwrite: true);

            try
            {
                updateProgress?.Invoke(50, "Заполнение данных в Word...");

                using (var doc = WordprocessingDocument.Open(tempPath, true))
                {
                    UpdateHeaderText(doc, awardsText, periodText);

                    var table = doc.MainDocumentPart?.Document.Body?.Elements<Word.Table>().FirstOrDefault();

                    if (table != null)
                    {
                        var rows = table.Elements<Word.TableRow>().ToList();

                        if (rows.Count > 1)
                        {
                            for (int i = 1; i < rows.Count; i++)
                            {
                                rows[i].Remove();
                            }
                        }

                        int rowNum = 1;
                        foreach (var item in awardings)
                        {
                            Word.TableRow row = new Word.TableRow();

                            AppendTableCell(row, rowNum.ToString());
                            AppendTableCell(row, item.AwardedName);
                            AppendTableCell(row, item.Position);
                            AppendTableCell(row, item.Reason);
                            AppendTableCell(row, item.DecreeNumber);
                            AppendTableCell(row, item.DecreeDate.ToString("dd.MM.yyyy"));

                            table.Append(row);
                            rowNum++;
                        }
                    }
                    doc.MainDocumentPart?.Document.Save();
                }

                updateProgress?.Invoke(95, "Сохранение документа...");
                File.Copy(tempPath, filePath, overwrite: true);
                updateProgress?.Invoke(100, "Готово!");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private void UpdateHeaderText(WordprocessingDocument doc, string awardsText, string periodText)
        {
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return;

            foreach (var paragraph in body.Descendants<Word.Paragraph>())
            {
                string text = string.Concat(paragraph.Descendants<Word.Text>().Select(t => t.Text));

                if (text.Contains("Благодарственными письмами"))
                {
                    foreach (var run in paragraph.Descendants<Word.Text>())
                    {
                        run.Text = run.Text.Replace("Благодарственными письмами", awardsText);
                    }
                }
                if (text.Contains("2022 год"))
                {
                    foreach (var run in paragraph.Descendants<Word.Text>())
                    {
                        run.Text = run.Text.Replace("2022 год", periodText);
                    }
                }
            }
        }

        private void AppendTableCell(Word.TableRow row, string text, string fontName = "Times New Roman", int fontSize = 14)
        {
            Word.TableCell cell = new Word.TableCell();
            Word.Paragraph paragraph = new Word.Paragraph();
            Word.Run run = new Word.Run();

            run.RunProperties = new Word.RunProperties(
                new Word.RunFonts
                {
                    Ascii = fontName,
                    HighAnsi = fontName,
                },
                new Word.FontSize
                {
                    Val = (fontSize * 2).ToString()
                }
            );

            run.Append(new Word.Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);
            row.Append(cell);
        }

        public void CreateZipArchive(string excelPath, string wordPath, string zipPath, bool hasExcel, bool hasWord, string? password = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempDir);

                string tempExcelPath = Path.Combine(tempDir, Path.GetFileName(excelPath));
                string tempWordPath = Path.Combine(tempDir, Path.GetFileName(wordPath));

                if (hasExcel && File.Exists(excelPath))
                {
                    File.Copy(excelPath, tempExcelPath, true);
                }

                if (hasWord && File.Exists(wordPath))
                {
                    File.Copy(wordPath, tempWordPath, true);
                }

                using (var zipStream = new FileStream(zipPath, FileMode.Create))
                using (var zipFile = new ZipOutputStream(zipStream))
                {
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        zipFile.Password = password;
                    }
                    zipFile.SetLevel(9);

                    if (hasExcel && File.Exists(tempExcelPath))
                    {
                        var entry = new ZipEntry(Path.GetFileName(excelPath));
                        zipFile.PutNextEntry(entry);
                        byte[] buffer = File.ReadAllBytes(tempExcelPath);
                        zipFile.Write(buffer, 0, buffer.Length);
                        zipFile.CloseEntry();
                    }

                    if (hasWord && File.Exists(tempWordPath))
                    {
                        var entry = new ZipEntry(Path.GetFileName(wordPath));
                        zipFile.PutNextEntry(entry);
                        byte[] buffer = File.ReadAllBytes(tempWordPath);
                        zipFile.Write(buffer, 0, buffer.Length);
                        zipFile.CloseEntry();
                    }
                }

                try
                {
                    if (hasExcel && File.Exists(excelPath)) File.Delete(excelPath);
                    if (hasWord && File.Exists(wordPath)) File.Delete(wordPath);
                }
                catch { }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
