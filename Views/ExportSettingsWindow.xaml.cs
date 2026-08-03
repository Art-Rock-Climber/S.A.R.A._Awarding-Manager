using DocumentFormat.OpenXml.Packaging;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using OfficeOpenXml;
using sara_coursework.data;
using sara_coursework.models;
using sara_coursework.ViewModels;
using sara_coursework.Services.Security;
using sara_coursework.Services;
using sara_coursework.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Word = DocumentFormat.OpenXml.Wordprocessing; // Алиас для Wordprocessing

namespace sara_coursework.Views
{
    /// <summary>
    /// Логика взаимодействия для ExportSettingsWindow.xaml
    /// </summary>
    public partial class ExportSettingsWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public ExportSettingsWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateExportSettings())
            {
                MessageBox.Show("Пожалуйста, проверьте настройки экспорта", "Ошибка");
                return;
            }

            try
            {
                bool exportExcel = chkExcel.IsChecked ?? false;
                bool exportWord = chkWord.IsChecked ?? false;

                if (!exportExcel && !exportWord)
                {
                    MessageBox.Show("Выберите хотя бы один формат экспорта");
                    return;
                }


                var saveDialog = new SaveFileDialog
                {
                    FileName = "Награждения_" + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss"),
                    Filter = "ZIP архив (*.zip)|*.zip"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string basePath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                    string baseName = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    string excelPath = System.IO.Path.Combine(basePath, baseName + ".xlsx");
                    string wordPath = System.IO.Path.Combine(basePath, baseName + ".docx");
                    string zipPath = saveDialog.FileName;

                    // Настройка прогресса
                    progressBar.Maximum = 100;
                    progressBar.Value = 0;
                    txtProgress.Text = "Подготовка к экспорту...";
                    string? zipPassword = chkEnablePassword.IsChecked == true ? txtZipPassword.Password : null;

                    await Task.Run(() => PerformExport(exportExcel, exportWord,
                        excelPath, wordPath, zipPath, zipPassword));

                    MessageBox.Show("Экспорт успешно завершен!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PerformExport(bool exportExcel, bool exportWord,
            string excelPath, string wordPath, string zipPath, string? zipPassword)
        {
            Dispatcher.Invoke(() => txtProgress.Text = "Экспорт данных...");

            if (exportExcel)
            {
                Dispatcher.Invoke(() => txtProgress.Text = "Экспорт в Excel...");
                ExportToExcel(excelPath);
                Dispatcher.Invoke(() => progressBar.Value += 50);
            }

            if (exportWord)
            {
                Dispatcher.Invoke(() => txtProgress.Text = "Экспорт в Word...");
                ExportToWord(wordPath);
                Dispatcher.Invoke(() => progressBar.Value += 50);
            }

            if (exportExcel || exportWord)
            {
                Dispatcher.Invoke(() => txtProgress.Text = "Создание архива...");
                CreateZipArchive(excelPath, wordPath, zipPath, exportExcel, exportWord, zipPassword);
                Dispatcher.Invoke(() => progressBar.Value = 100);
            }

            Dispatcher.Invoke(() => txtProgress.Text = "Экспорт завершен");
        }

        private void UpdateProgress(int value, string message)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = value;
                txtProgress.Text = message;
            });
        }

        private void ExportToExcel(string filePath)
        {
            // Получаем выбранные листы в UI-потоке
            List<string> selectedSheets = Dispatcher.Invoke(() =>
            {
                return lbExcelSheets.SelectedItems.OfType<ListBoxItem>()
                    .Select(x => x.Content?.ToString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x!)
                    .ToList();
            });
            bool? needStats = Dispatcher.Invoke(() =>
            {
                return chkExcelStats.IsChecked;
            });

            using (var package = new ExcelPackage())
            {
                int totalItems = selectedSheets.Count;
                int currentSheet = 0;

                var assignmentRepo = new AwardAssignmentRepository();
                var awardedRepo = new AwardedRepository();
                var decreeRepo = new DecreeRepository();
                var awardRepo = new AwardRepository();
                var reasonRepo = new AwardReasonRepository();

                // Награждения
                if (selectedSheets.Contains("Награждения"))
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Экспорт награждений...");
                    var ws = package.Workbook.Worksheets.Add("Награждения");

                    ws.Cells[1, 1].Value = "ID";
                    ws.Cells[1, 2].Value = "ID Награждаемого";
                    ws.Cells[1, 3].Value = "Награждаемый";
                    ws.Cells[1, 4].Value = "ID Награды";
                    ws.Cells[1, 5].Value = "Награда";
                    ws.Cells[1, 6].Value = "ID Постановления";
                    ws.Cells[1, 7].Value = "Постановление";

                    int row = 2;
                    foreach (var item in assignmentRepo.GetAwardAssignments())
                    {
                        ws.Cells[row, 1].Value = item.Id;
                        ws.Cells[row, 2].Value = item.AwardedId;
                        ws.Cells[row, 3].Value = item.Awarded is Citizen c ? c.ToString() : ((Collective)item.Awarded).CollectiveName;
                        ws.Cells[row, 4].Value = item.AwardId;
                        ws.Cells[row, 5].Value = item.Award?.AwardName;
                        ws.Cells[row, 6].Value = item.DecreeId;
                        ws.Cells[row, 7].Value = item.Decree?.DisplayText;
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                // Награждаемые
                if (selectedSheets.Contains("Награждаемые"))
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Экспорт награждаемых...");
                    var ws = package.Workbook.Worksheets.Add("Награждаемые");

                    ws.Cells[1, 1].Value = "ID";
                    ws.Cells[1, 2].Value = "Тип";
                    ws.Cells[1, 3].Value = "ФИО/Название";
                    ws.Cells[1, 4].Value = "Фамилия";
                    ws.Cells[1, 5].Value = "Имя";
                    ws.Cells[1, 6].Value = "Отчество";
                    ws.Cells[1, 7].Value = "Должность";
                    ws.Cells[1, 8].Value = "ID Коллектива";

                    int row = 2;
                    foreach (var item in awardedRepo.GetAwarded())
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
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                // Постановления
                if (selectedSheets.Contains("Постановления"))
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Экспорт постановлений...");
                    var ws = package.Workbook.Worksheets.Add("Постановления");

                    ws.Cells[1, 1].Value = "ID";
                    ws.Cells[1, 2].Value = "Номер приказа";
                    ws.Cells[1, 3].Value = "Дата приказа";
                    ws.Cells[1, 4].Value = "ID Основания";
                    ws.Cells[1, 5].Value = "Основание";

                    int row = 2;
                    foreach (var item in decreeRepo.GetDecrees())
                    {
                        ws.Cells[row, 1].Value = item.Id;
                        ws.Cells[row, 2].Value = item.Number;
                        ws.Cells[row, 3].Value = item.Date.ToString("dd.MM.yyyy");
                        ws.Cells[row, 4].Value = item.AwardReasonId;
                        ws.Cells[row, 5].Value = item.AwardReason?.ReasonName;
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                // Награды
                if (selectedSheets.Contains("Награды"))
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Экспорт наград...");
                    var ws = package.Workbook.Worksheets.Add("Награды");

                    ws.Cells[1, 1].Value = "ID";
                    ws.Cells[1, 2].Value = "Награда";

                    int row = 2;
                    foreach (var item in awardRepo.GetAwards())
                    {
                        ws.Cells[row, 1].Value = item.Id;
                        ws.Cells[row, 2].Value = item.AwardName;
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                // Основания
                if (selectedSheets.Contains("Основания"))
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Экспорт оснований...");
                    var ws = package.Workbook.Worksheets.Add("Основания");

                    ws.Cells[1, 1].Value = "ID";
                    ws.Cells[1, 2].Value = "Основание";

                    int row = 2;
                    foreach (var item in reasonRepo.GetAwardReasons())
                    {
                        ws.Cells[row, 1].Value = item.Id;
                        ws.Cells[row, 2].Value = item.ReasonName;
                        row++;
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                // Статистика
                if (needStats == true)
                {
                    UpdateProgress(++currentSheet * 100 / totalItems, "Формирование статистики...");
                    var ws = package.Workbook.Worksheets.Add("Статистика");

                    var allAwardings = _mainViewModel.GetAllAwardingsVmFromDatabase();
                    var currentDate = DateTime.Now;

                    // Количество награждённых за месяц, квартал, год
                    ws.Cells[1, 1].Value = "Период";
                    ws.Cells[1, 2].Value = "Кол-во награждений";
                    ws.Cells[1, 3].Value = "Примечание";
                    int row = 2;

                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthCount = allAwardings.Count(a => a.DecreeDate >= monthStart && a.DecreeDate <= currentDate);
                    ws.Cells[row, 1].Value = "Текущий месяц";
                    ws.Cells[row, 2].Value = monthCount;
                    ws.Cells[row, 3].Value = $"{monthStart:MMMM yyyy}";
                    row++;

                    int currentQuarter = (currentDate.Month - 1) / 3 + 1;
                    var quarterStart = new DateTime(currentDate.Year, (currentQuarter - 1) * 3 + 1, 1);
                    var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
                    var quarterCount = allAwardings.Count(a => a.DecreeDate >= quarterStart && a.DecreeDate <= quarterEnd);
                    ws.Cells[row, 1].Value = "Текущий квартал";
                    ws.Cells[row, 2].Value = quarterCount;
                    ws.Cells[row, 3].Value = $"{quarterStart:MMMM} - {quarterEnd:MMMM yyyy}";
                    row++;

                    var yearStart = new DateTime(currentDate.Year, 1, 1);
                    var yearCount = allAwardings.Count(a => a.DecreeDate.Year == currentDate.Year);
                    ws.Cells[row, 1].Value = "Текущий год";
                    ws.Cells[row, 2].Value = yearCount;
                    ws.Cells[row, 3].Value = $"{currentDate.Year} год";

                    // Количество награждённых за год по награждённым
                    row += 2;
                    ws.Cells[row, 1].Value = "Награждаемый";
                    ws.Cells[row, 2].Value = "Тип";
                    ws.Cells[row, 3].Value = "Кол-во награждений";
                    ws.Cells[row, 4].Value = "Год";
                    row++;

                    var orgStats = allAwardings
                        .Where(a => a.DecreeDate.Year == currentDate.Year)
                        .GroupBy(a => new { a.AwardedName, a.AwardedType })
                        .Select(g => new { Awarded = g.Key.AwardedName, Type = g.Key.AwardedType, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    foreach (var stat in orgStats)
                    {
                        ws.Cells[row, 1].Value = stat.Awarded;
                        ws.Cells[row, 2].Value = stat.Type;
                        ws.Cells[row, 3].Value = stat.Count;
                        ws.Cells[row, 4].Value = currentDate.Year;
                        row++;
                    }

                    // Форматирование
                    ws.Cells[1, 1, 1, 3].Style.Font.Bold = true;
                    ws.Cells[6, 1, 6, 3].Style.Font.Bold = true;
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    // Добавляем график для визуализации
                    var chart = ws.Drawings.AddChart("СтатистикаНаграждений", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
                    chart.SetPosition(1, 0, 5, 0);
                    chart.SetSize(800, 400);

                    // Серия данных для организаций
                    var serie = chart.Series.Add(ws.Cells[7, 3, 7 + orgStats.Count - 1, 3], ws.Cells[7, 1, 7 + orgStats.Count - 1, 1]);
                    serie.Header = "Количество награждений по организациям";

                    chart.Title.Text = "Статистика награждений за текущий год";
                }

                UpdateProgress(100, "Сохранение Excel файла...");
                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportToWord(string filePath)
        {
            // Получаем шаблон из ресурсов
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("template.docx"));

            if (string.IsNullOrEmpty(resourceName))
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show("Шаблон документа не найден", "Ошибка"));
                return;
            }

            // Создаем временный файл
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".docx");

            try
            {
                UpdateProgress(0, "Подготовка шаблона...");

                // Копируем шаблон
                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                using (FileStream fileStream = File.Create(tempPath))
                {
                    resourceStream?.CopyTo(fileStream);
                }

                UpdateProgress(30, "Заполнение документа...");

                // Редактируем документ
                using (WordprocessingDocument doc = WordprocessingDocument.Open(tempPath, true))
                {
                    // Обновляем период в заголовке
                    string awardsText = _mainViewModel.GetSelectedAwardsText();
                    string periodText = _mainViewModel.GetPeriodTextFromFilters();
                    UpdateHeaderText(doc, awardsText, periodText);

                    // Заполняем таблицу данными
                    var body = doc.MainDocumentPart.Document.Body;
                    
                    // Поиск таблицы с заголовками для надежности
                    Word.Table table = null;
                    foreach (var t in body.Descendants<Word.Table>())
                    {
                        var text = t.InnerText ?? "";
                        if (text.Contains("ФИО") || text.Contains("Должность") || text.Contains("Постановление") || text.Contains("Приказ"))
                        {
                            table = t;
                            break;
                        }
                    }
                    if (table == null)
                    {
                        table = body.Descendants<Word.Table>().FirstOrDefault();
                    }

                    if (table != null)
                    {
                        // Получаем шаблонную строку (вторую строку таблицы) для клонирования стилей
                        var rows = table.Elements<Word.TableRow>().ToList();
                        Word.TableRow templateRow = null;
                        if (rows.Count > 1)
                        {
                            templateRow = (Word.TableRow)rows[1].CloneNode(true);
                            // Очищаем существующие строки (кроме заголовков)
                            while (table.Elements<Word.TableRow>().Count() > 1)
                            {
                                table.RemoveChild(table.Elements<Word.TableRow>().Last());
                            }
                        }

                        bool? onlyFiltered = Dispatcher.Invoke(() =>
                        {
                            return chkWordFiltered.IsChecked;
                        });

                        IEnumerable<AwardingViewModel> itemsToExport = onlyFiltered == true
                            ? _mainViewModel.AwardingsTab.Awardings // Отфильтрованные данные
                            : _mainViewModel.GetAllAwardingsVmFromDatabase(); // Все данные из БД

                        // Добавляем данные
                        int rowNum = 1;
                        foreach (AwardingViewModel item in itemsToExport)
                        {
                            UpdateProgress(30 + (rowNum * 60 / itemsToExport.Count()),
                                $"Добавление записи {rowNum}...");

                            Word.TableRow row;
                            if (templateRow != null)
                            {
                                row = (Word.TableRow)templateRow.CloneNode(true);
                                var cells = row.Elements<Word.TableCell>().ToList();
                                // if (cells.Count > 0) FillCellText(cells[0], rowNum.ToString());
                                if (cells.Count > 1) FillCellText(cells[1], item.AwardedName);
                                if (cells.Count > 2) FillCellText(cells[2], item.Position);
                                if (cells.Count > 3) FillCellText(cells[3], item.Reason);
                                if (cells.Count > 4) FillCellText(cells[4], item.DecreeNumber);
                                if (cells.Count > 5) FillCellText(cells[5], item.DecreeDate.ToString("dd.MM.yyyy"));
                            }
                            else
                            {
                                row = new Word.TableRow();
                                AppendTableCell(row, rowNum.ToString());
                                AppendTableCell(row, item.AwardedName);
                                AppendTableCell(row, item.Position);
                                AppendTableCell(row, item.Reason);
                                AppendTableCell(row, item.DecreeNumber);
                                AppendTableCell(row, item.DecreeDate.ToString("dd.MM.yyyy"));
                            }

                            table.Append(row);
                            rowNum++;
                        }
                    }
                    doc.MainDocumentPart.Document.Save();
                }

                UpdateProgress(95, "Сохранение документа...");
                File.Copy(tempPath, filePath, overwrite: true);
                UpdateProgress(100, "Готово!");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private void UpdateHeaderText(WordprocessingDocument doc, string awardsText, string periodText)
        {
            var body = doc.MainDocumentPart.Document.Body;
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
                    Val = (fontSize * 2).ToString() // Конвертация в OpenXML-формат удвоенных пунктов
                }
            );

            run.Append(new Word.Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);
            row.Append(cell);
        }

        private void FillCellText(Word.TableCell cell, string text)
        {
            var runs = cell.Descendants<Word.Run>().ToList();
            if (runs.Count > 0)
            {
                // Устанавливаем текст первого Run, сохраняя свойства RunProperties
                var firstRun = runs[0];
                var textNode = firstRun.ChildElements.OfType<Word.Text>().FirstOrDefault();
                if (textNode != null)
                {
                    textNode.Text = text;
                }
                else
                {
                    firstRun.AppendChild(new Word.Text(text));
                }

                // ... удалить другие Run-элементы, чтобы не дублировать текст
                for (int i = 1; i < runs.Count; i++)
                {
                    runs[i].Remove();
                }

                // Очищаем лишние текстовые узлы внутри первого Run
                var extraTexts = firstRun.ChildElements.OfType<Word.Text>().Skip(1).ToList();
                foreach (var extra in extraTexts)
                {
                    extra.Remove();
                }
            }
            else
            {
                var paragraph = cell.Elements<Word.Paragraph>().FirstOrDefault();
                if (paragraph == null)
                {
                    paragraph = new Word.Paragraph();
                    cell.AppendChild(paragraph);
                }
                var run = new Word.Run();
                run.AppendChild(new Word.Text(text));
                paragraph.AppendChild(run);
            }
        }

        private void CreateZipArchive(string excelPath, string wordPath, string zipPath,
            bool hasExcel, bool hasWord, string? zipPassword)
        {
            var exportService = new ExportService();
            exportService.CreateZipArchive(excelPath, wordPath, zipPath, hasExcel, hasWord, zipPassword);
        }

        private void ExcelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (lbExcelSheets == null) return;

            // Автоматически выбираем все листы, если Excel включен
            if (chkExcel.IsChecked == true)
            {
                lbExcelSheets.SelectAll();
            }
            else
            {
                lbExcelSheets.SelectedItems.Clear();
            }
        }

        private bool ValidateExportSettings()
        {
            bool isValid = true;

            if (chkExcel.IsChecked == true && lbExcelSheets.SelectedItems.Count == 0)
            {
                txtExcelWarning.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtExcelWarning.Visibility = Visibility.Collapsed;
            }

            if (chkEnablePassword.IsChecked == true)
            {
                string pass = txtZipPassword.Password;
                string confirm = txtZipPasswordConfirm.Password;

                if (string.IsNullOrWhiteSpace(pass))
                {
                    MessageBox.Show("Пожалуйста, введите пароль для архива", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (pass != confirm)
                {
                    MessageBox.Show("Пароль и подтверждение пароля не совпадают", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return isValid;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
