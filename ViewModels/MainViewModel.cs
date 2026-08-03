using Microsoft.Win32;
using OfficeOpenXml;
using sara_coursework.data;
using sara_coursework.models;
using sara_coursework.Services.Repositories;
using sara_coursework.ViewModels.Tabs;
using sara_coursework.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Repositories
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly IAwardedRepository _awardedRepo;
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardRepository _awardRepo;
        private readonly IAwardReasonRepository _reasonRepo;
        private readonly ILogRepository _logRepo;
        private readonly IUserRepository _userRepo;

        private User? _currentUser;
        private int _selectedTabIndex;

        // Child ViewModels (composition)
        public AwardingsTabViewModel AwardingsTab { get; }
        public AwardedTabViewModel AwardedTab { get; }
        public DecreesTabViewModel DecreesTab { get; }
        public AwardsTabViewModel AwardsTab { get; }
        public ReasonsTabViewModel ReasonsTab { get; }
        public UsersTabViewModel UsersTab { get; }
        public LogsTabViewModel LogsTab { get; }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(CanCRUD));
                    OnPropertyChanged(nameof(CanExport));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public bool CanCRUD => CurrentUser != null && CurrentUser.Role != UserRole.ReadOnly;
        public bool CanExport => CurrentUser != null;

        public string WindowTitle => CurrentUser != null
            ? $"S.A.R.A. - {CurrentUser.Username} ({CurrentUser.Role})"
            : "S.A.R.A. - Не авторизован";

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnTabSelected(value);
                }
            }
        }

        private void OnTabSelected(int index)
        {
            switch (index)
            {
                case 0:
                    AwardingsTab?.LoadData();
                    AwardingsTab?.LoadFilters();
                    break;
                case 1:
                    AwardedTab?.LoadData();
                    break;
                case 2:
                    DecreesTab?.LoadData();
                    break;
                case 3:
                    AwardsTab?.LoadData();
                    break;
                case 4:
                    ReasonsTab?.LoadData();
                    break;
                case 5:
                    UsersTab?.LoadData();
                    break;
                case 6:
                    LogsTab?.LoadData();
                    break;
            }
        }

        // Global Commands
        public ICommand ExportToZipCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand OpenHelpCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public MainViewModel(User currentUser)
        {
            CurrentUser = currentUser;

            // Instantiate repositories
            _assignmentRepo = new AwardAssignmentRepository();
            _awardedRepo = new AwardedRepository();
            _decreeRepo = new DecreeRepository();
            _awardRepo = new AwardRepository();
            _reasonRepo = new AwardReasonRepository();
            _logRepo = new LogRepository();
            _userRepo = new UserRepository();

            // Set up reloading/cross-tab triggers
            Action reloadLogs = () => LogsTab.LoadData();
            Action reloadAwardings = () => AwardingsTab.LoadData();
            Action reloadFilters = () => AwardingsTab.LoadFilters();
            Action reloadDecrees = () => DecreesTab.LoadData();
            Action reloadAllRelated = () =>
            {
                AwardedTab?.LoadData();
                DecreesTab?.LoadData();
                AwardsTab?.LoadData();
                AwardingsTab?.LoadFilters();
            };

            // Instantiate child ViewModels
            AwardingsTab = new AwardingsTabViewModel(
                _assignmentRepo, _awardRepo, _decreeRepo, _logRepo, () => CurrentUser, reloadLogs, reloadAllRelated);

            AwardedTab = new AwardedTabViewModel(
                _awardedRepo, _assignmentRepo, _logRepo, () => CurrentUser, reloadLogs, reloadAwardings);

            DecreesTab = new DecreesTabViewModel(
                _decreeRepo, _reasonRepo, _logRepo, () => CurrentUser, reloadLogs, reloadAwardings);

            AwardsTab = new AwardsTabViewModel(
                _awardRepo, _logRepo, () => CurrentUser, reloadLogs, reloadFilters, reloadAwardings);

            ReasonsTab = new ReasonsTabViewModel(
                _reasonRepo, _logRepo, () => CurrentUser, reloadLogs, reloadDecrees);

            UsersTab = new UsersTabViewModel(_userRepo, _logRepo, () => CurrentUser);
            LogsTab = new LogsTabViewModel(_logRepo, () => CurrentUser);

            // Wire global commands
            ExportToZipCommand = new RelayCommand(o => ExecuteExport(), o => CanExport);
            ImportFromExcelCommand = new RelayCommand(o => ExecuteImport(), o => IsAdmin);
            LoginCommand = new RelayCommand(ExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister);
            OpenHelpCommand = new RelayCommand(ExecuteOpenHelp);
            AddCommand = new RelayCommand(o => ExecuteAdd(), o => CanCRUD);
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => CanCRUD);
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => CanCRUD);

            // Initial load
            LoadAllData();
        }

        public void LoadAllData()
        {
            AwardingsTab.LoadData();
            AwardingsTab.LoadFilters();
            AwardedTab.LoadData();
            DecreesTab.LoadData();
            AwardsTab.LoadData();
            ReasonsTab.LoadData();
            UsersTab.LoadData();
            LogsTab.LoadData();
        }

        private void ExecuteExport()
        {
            var exportWindow = new ExportSettingsWindow(this);
            exportWindow.Owner = Application.Current.MainWindow;
            exportWindow.ShowDialog();
        }

        private void ExecuteLogin()
        {
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                CurrentUser = loginWindow.CurrentUser;
                LoadAllData();
                _logRepo.LogAction("Info", "Login",
                    $"Пользователь {CurrentUser.Username} вошел в систему.",
                    CurrentUser.Username);
                LogsTab.LoadData();
            }
        }

        private void ExecuteRegister()
        {
            var registerWindow = new RegisterWindow();
            if (registerWindow.ShowDialog() == true)
            {
                _logRepo.LogAction("Info", "Register",
                    "Зарегистрирован новый пользователь.",
                    CurrentUser?.Username ?? "System");
                LogsTab.LoadData();
            }
        }

        private void ExecuteOpenHelp()
        {
            string helpFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "help");
            string mainHelpFile = System.IO.Path.Combine(helpFolder, "help.chm");

            if (System.IO.File.Exists(mainHelpFile))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mainHelpFile) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Файл руководства не найден!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Export Helper getters called by ExportSettingsWindow
        public string GetPeriodTextFromFilters()
        {
            if (AwardingsTab.StartDate.HasValue && AwardingsTab.EndDate.HasValue)
            {
                int startYear = AwardingsTab.StartDate.Value.Year;
                int endYear = AwardingsTab.EndDate.Value.Year;

                return startYear == endYear ? $"{startYear} год" : $"{startYear}-{endYear} годы";
            }
            return $"{DateTime.Now.Year} год";
        }

        public string GetSelectedAwardsText()
        {
            var selectedAwards = AwardingsTab.AwardFilterItems.Where(a => a.IsSelected).Select(a => a.AwardName).ToList();
            return selectedAwards.Count > 0 ? string.Join(", ", selectedAwards) : "всех типов";
        }

        public List<AwardingViewModel> GetAllAwardingsVmFromDatabase()
        {
            return _assignmentRepo.GetAwardAssignments().Select(aa => new AwardingViewModel(aa)).ToList();
        }

        private void ExecuteAdd()
        {
            switch (SelectedTabIndex)
            {
                case 0: AwardingsTab.AddCommand.Execute(null); break;
                case 2: AwardedTab.AddCommand.Execute(null); break;
                case 3: DecreesTab.AddCommand.Execute(null); break;
                case 4: AwardsTab.AddCommand.Execute(null); break;
                case 5: ReasonsTab.AddCommand.Execute(null); break;
                case 7: UsersTab.AddCommand.Execute(null); break;
            }
        }

        private void ExecuteEdit()
        {
            switch (SelectedTabIndex)
            {
                case 0: AwardingsTab.EditCommand.Execute(null); break;
                case 2: AwardedTab.EditCommand.Execute(null); break;
                case 3: DecreesTab.EditCommand.Execute(null); break;
                case 4: AwardsTab.EditCommand.Execute(null); break;
                case 5: ReasonsTab.EditCommand.Execute(null); break;
                case 7: UsersTab.EditCommand.Execute(null); break;
            }
        }

        private void ExecuteDelete()
        {
            switch (SelectedTabIndex)
            {
                case 0: AwardingsTab.DeleteCommand.Execute(null); break;
                case 2: AwardedTab.DeleteCommand.Execute(null); break;
                case 3: DecreesTab.DeleteCommand.Execute(null); break;
                case 4: AwardsTab.DeleteCommand.Execute(null); break;
                case 5: ReasonsTab.DeleteCommand.Execute(null); break;
                case 7: UsersTab.DeleteCommand.Execute(null); break;
            }
        }

        private void ExecuteImport()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                Title = "Выберите Excel-файл для импорта базы данных"
            };

            if (openDialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "Выберите режим импорта базы данных:\n\n" +
                    "• Нажмите [Да], чтобы ПОЛНОСТЬЮ ОЧИСТИТЬ базу данных перед импортом (все существующие данные о награждениях будут удалены).\n" +
                    "• Нажмите [Нет], чтобы ОБЪЕДИНИТЬ данные (существующие записи останутся, новые добавятся).\n" +
                    "• Нажмите [Отмена] для отмены операции.",
                    "Выбор режима импорта",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.Yes)
                {
                    var confirmClear = MessageBox.Show(
                        "ВНИМАНИЕ: Вы выбрали полную перезапись базы данных. Все текущие записи о награждениях, наградах, приказах и т.д. будут безвозвратно удалены!\n\n" +
                        "Вы действительно хотите продолжить?",
                        "Предупреждение о потере данных",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Stop);

                    if (confirmClear != MessageBoxResult.Yes) return;
                }

                string filePath = openDialog.FileName;
                ImportDatabaseFromExcel(filePath, result == MessageBoxResult.Yes);
            }
        }

        private void ImportDatabaseFromExcel(string filePath, bool clearDatabase)
        {
            try
            {
                var importService = new sara_coursework.Services.ImportService();
                importService.ImportDatabaseFromExcel(filePath, clearDatabase);

                // Reload GUI
                LoadAllData();
                _logRepo.LogAction("Info", "ImportDatabase",
                    $"Пользователь {CurrentUser?.Username ?? "System"} успешно импортировал базу данных из файла {Path.GetFileName(filePath)} (Режим очистки: {clearDatabase}).",
                    CurrentUser?.Username ?? "System");
                LogsTab.LoadData();

                MessageBox.Show("Импорт базы данных успешно завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
