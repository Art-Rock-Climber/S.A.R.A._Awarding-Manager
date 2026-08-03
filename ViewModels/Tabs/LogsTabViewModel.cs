using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels.Tabs
{
    public class LogsTabViewModel : ViewModelBase
    {
        private readonly ILogRepository _logRepo;
        private readonly Func<User?>? _getCurrentUser;

        private DateTime? _logStartDate;
        private DateTime? _logEndDate;
        private string _selectedLogLevel = "Все уровни";

        private string _searchText = string.Empty;
        private List<LogEntry> _allLogs = new();

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalItems;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    ApplyLogFilters();
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyLogFilters();
                }
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            private set
            {
                if (SetProperty(ref _totalItems, value))
                {
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyLogFilters();
                }
            }
        }

        public ObservableCollection<LogEntry> Logs { get; } = new();

        public DateTime? LogStartDate
        {
            get => _logStartDate;
            set => SetProperty(ref _logStartDate, value);
        }

        public DateTime? LogEndDate
        {
            get => _logEndDate;
            set => SetProperty(ref _logEndDate, value);
        }

        public string SelectedLogLevel
        {
            get => _selectedLogLevel;
            set => SetProperty(ref _selectedLogLevel, value);
        }

        public ICommand ApplyLogFiltersCommand { get; }
        public ICommand ResetLogFiltersCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public LogsTabViewModel(ILogRepository logRepo, Func<User?>? getCurrentUser = null)
        {
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;

            ApplyLogFiltersCommand = new RelayCommand(ApplyLogFilters);
            ResetLogFiltersCommand = new RelayCommand(ResetLogFilters);
            ClearLogsCommand = new RelayCommand(o => ExecuteClearLogs(), o => IsAdmin());
            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });

            _logStartDate = DateTime.Today.AddYears(-1);
            _logEndDate = DateTime.Today;
        }

        private bool IsAdmin() => _getCurrentUser == null || _getCurrentUser()?.Role == UserRole.Admin;

        private void ExecuteClearLogs()
        {
            if (!LogStartDate.HasValue || !LogEndDate.HasValue)
            {
                MessageBox.Show("Укажите начальную и конечную даты периода для очистки логов.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string periodText = $"{LogStartDate.Value:dd.MM.yyyy} по {LogEndDate.Value:dd.MM.yyyy}";
            var result = MessageBox.Show($"Вы действительно хотите безвозвратно удалить все записи журнала за период с {periodText}?",
                "Подтверждение очистки", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _logRepo.ClearLogs(LogStartDate.Value, LogEndDate.Value);
                    string currentUser = _getCurrentUser?.Invoke()?.Username ?? "System";
                    _logRepo.LogAction("Warning", "ClearLogs", $"Пользователь {currentUser} очистил журнал за период с {periodText}.", currentUser);
                    LoadData();
                    MessageBox.Show($"Логи за период с {periodText} успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при очистке журнала: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LoadData()
        {
            try
            {
                _allLogs = _logRepo.GetLogs();
                ApplyLogFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки журнала: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyLogFilters()
        {
            try
            {
                var query = _allLogs.AsQueryable();

                if (LogStartDate.HasValue)
                    query = query.Where(l => l.Timestamp >= LogStartDate.Value);

                if (LogEndDate.HasValue)
                    query = query.Where(l => l.Timestamp <= LogEndDate.Value.AddDays(1));

                if (!string.IsNullOrEmpty(SelectedLogLevel) && SelectedLogLevel != "Все уровни")
                {
                    query = query.Where(l => l.Level == SelectedLogLevel);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var text = SearchText.ToLower();
                    query = query.Where(l =>
                        (l.Message != null && l.Message.ToLower().Contains(text)) ||
                        (l.Action != null && l.Action.ToLower().Contains(text)) ||
                        (l.UserName != null && l.UserName.ToLower().Contains(text))
                    );
                }

                var list = query.OrderByDescending(x => x.Timestamp).ToList();
                TotalItems = list.Count;

                if (_currentPage > TotalPages) _currentPage = TotalPages;
                if (_currentPage < 1) _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));

                var paged = list
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                Logs.Clear();
                foreach (var l in paged)
                {
                    Logs.Add(l);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации логов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetLogFilters()
        {
            _logStartDate = DateTime.Today.AddYears(-1);
            _logEndDate = DateTime.Today;
            _selectedLogLevel = "Все уровни";
            _searchText = string.Empty;
            _currentPage = 1;

            OnPropertyChanged(nameof(LogStartDate));
            OnPropertyChanged(nameof(LogEndDate));
            OnPropertyChanged(nameof(SelectedLogLevel));
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(CurrentPage));

            ApplyLogFilters();
        }
    }
}
