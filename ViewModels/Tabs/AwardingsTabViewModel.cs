using sara_coursework.models;
using sara_coursework.Services.Repositories;
using sara_coursework.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels.Tabs
{
    public class AwardingsTabViewModel : ViewModelBase
    {
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly IAwardRepository _awardRepo;
        private readonly IDecreeRepository _decreeRepo;
        private readonly ILogRepository _logRepo;
        private readonly Func<User?> _getCurrentUser;
        private readonly Action _reloadLogs;
        private readonly Action? _reloadAllRelated;

        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _searchName = string.Empty;
        private string _searchPosition = string.Empty;
        private string _searchDecree = string.Empty;
        private AwardingViewModel? _selectedAwarding;
        private List<AwardAssignment> _allAwardAssignments = new();

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
                    ApplyFullFilters();
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
                    ApplyFullFilters();
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

        public ObservableCollection<AwardingViewModel> Awardings { get; } = new();
        public ObservableCollection<AwardFilterItem> AwardFilterItems { get; } = new();

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyFullFilters();
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyFullFilters();
                }
            }
        }

        public string SearchName
        {
            get => _searchName;
            set
            {
                if (SetProperty(ref _searchName, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyFullFilters();
                }
            }
        }

        public string SearchPosition
        {
            get => _searchPosition;
            set
            {
                if (SetProperty(ref _searchPosition, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyFullFilters();
                }
            }
        }

        public string SearchDecree
        {
            get => _searchDecree;
            set
            {
                if (SetProperty(ref _searchDecree, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    ApplyFullFilters();
                }
            }
        }

        public AwardingViewModel? SelectedAwarding
        {
            get => _selectedAwarding;
            set => SetProperty(ref _selectedAwarding, value);
        }

        public ICommand FilterCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public AwardingsTabViewModel(
            IAwardAssignmentRepository assignmentRepo,
            IAwardRepository awardRepo,
            IDecreeRepository decreeRepo,
            ILogRepository logRepo,
            Func<User?> getCurrentUser,
            Action reloadLogs,
            Action? reloadAllRelated = null)
        {
            _assignmentRepo = assignmentRepo;
            _awardRepo = awardRepo;
            _decreeRepo = decreeRepo;
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;
            _reloadLogs = reloadLogs;
            _reloadAllRelated = reloadAllRelated;

            FilterCommand = new RelayCommand(ApplyFullFilters);
            ResetCommand = new RelayCommand(ResetFilters);
            AddCommand = new RelayCommand(o => ExecuteAdd(), o => CanCRUD());
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => CanCRUD());
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => CanCRUD());
            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });

            _startDate = DateTime.Today.AddYears(-1);
            _endDate = DateTime.Today;
        }

        private bool CanCRUD() => _getCurrentUser() != null && _getCurrentUser()?.Role != UserRole.ReadOnly;

        public void LoadData()
        {
            try
            {
                _allAwardAssignments = _assignmentRepo.GetAwardAssignments();
                LoadFilters();
                ApplyFullFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных награждений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFilters()
        {
            try
            {
                // Save selection state of existing filters to preserve them if possible
                var activeSelections = AwardFilterItems.ToDictionary(x => x.Id, x => x.IsSelected);

                AwardFilterItems.Clear();
                var list = _awardRepo.GetAwards().OrderBy(a => a.AwardName);
                foreach (var a in list)
                {
                    bool isSelected = true;
                    if (activeSelections.TryGetValue(a.Id, out bool val))
                    {
                        isSelected = val;
                    }
                    AwardFilterItems.Add(new AwardFilterItem
                    {
                        Id = a.Id,
                        AwardName = a.AwardName,
                        IsSelected = isSelected
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ApplyFullFilters()
        {
            var selectedIds = AwardFilterItems.Where(a => a.IsSelected).Select(a => a.Id).ToList();

            IEnumerable<AwardAssignment> filtered = _allAwardAssignments;

            if (selectedIds.Count > 0)
            {
                filtered = filtered.Where(aa => selectedIds.Contains(aa.AwardId));
            }

            if (StartDate.HasValue)
            {
                filtered = filtered.Where(aa => aa.Decree.Date >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                filtered = filtered.Where(aa => aa.Decree.Date <= EndDate.Value);
            }

            if (!string.IsNullOrEmpty(SearchName))
            {
                string nameQuery = SearchName.Trim();
                filtered = filtered.Where(aa =>
                {
                    string displayName = aa.Awarded is Citizen citizen ? citizen.ToString() : ((Collective)aa.Awarded).CollectiveName;
                    return displayName.Contains(nameQuery, StringComparison.CurrentCultureIgnoreCase);
                });
            }

            if (!string.IsNullOrEmpty(SearchPosition))
            {
                string posQuery = SearchPosition.Trim();
                filtered = filtered.Where(aa =>
                {
                    string position = aa.Awarded is Citizen citizen ? citizen.Position : "-";
                    return position.Contains(posQuery, StringComparison.CurrentCultureIgnoreCase);
                });
            }

            if (!string.IsNullOrEmpty(SearchDecree))
            {
                string decQuery = SearchDecree.Trim();
                filtered = filtered.Where(aa =>
                {
                    string decreeNumber = aa.Decree?.Number ?? "";
                    return decreeNumber.Contains(decQuery, StringComparison.CurrentCultureIgnoreCase);
                });
            }

            var list = filtered.ToList();
            TotalItems = list.Count;

            if (_currentPage > TotalPages) _currentPage = TotalPages;
            if (_currentPage < 1) _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));

            var paged = list
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            Awardings.Clear();
            foreach (var item in paged)
            {
                Awardings.Add(new AwardingViewModel(item));
            }
        }

        private void ResetFilters()
        {
            foreach (var item in AwardFilterItems)
            {
                item.IsSelected = true;
            }
            _startDate = DateTime.Today.AddYears(-1);
            _endDate = DateTime.Today;
            _searchName = string.Empty;
            _searchPosition = string.Empty;
            _searchDecree = string.Empty;
            _currentPage = 1;

            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(SearchName));
            OnPropertyChanged(nameof(SearchPosition));
            OnPropertyChanged(nameof(SearchDecree));
            OnPropertyChanged(nameof(CurrentPage));

            ApplyFullFilters();
        }

        private void ExecuteAdd()
        {
            var addAwardWindow = new AddAwardAssignmentsWindow(_assignmentRepo, _awardRepo, _decreeRepo, new AwardedRepository());
            if (addAwardWindow.ShowDialog() == true)
            {
                LoadData();
                _logRepo.LogAction("Info", "AddAwarding",
                    $"Пользователь {_getCurrentUser()?.Username ?? "System"} добавил новое награждение.",
                    _getCurrentUser()?.Username ?? "System");
                _reloadLogs();
                _reloadAllRelated?.Invoke();
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedAwarding != null)
            {
                int editedId = SelectedAwarding.Id;
                var editWindow = new EditSingleAwardWindow(_assignmentRepo, _decreeRepo, _awardRepo, new AwardedRepository(), editedId);
                if (editWindow.ShowDialog() == true)
                {
                    _logRepo.LogAction("Info", "EditAwarding",
                        $"Пользователь {_getCurrentUser()?.Username ?? "System"} изменил награждение (ID: {editedId}).",
                        _getCurrentUser()?.Username ?? "System");
                    LoadData();
                    _reloadLogs();
                    _reloadAllRelated?.Invoke();
                }
            }
            else
            {
                MessageBox.Show("Выберите награждение для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedAwarding != null)
            {
                var res = MessageBox.Show("Удалить выбранное награждение?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        int idToDelete = SelectedAwarding.Id;
                        _assignmentRepo.DeleteAwardAssignment(idToDelete);
                        LoadData();
                        _logRepo.LogAction("Info", "DeleteAwarding",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} удалил награждение (ID: {idToDelete}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
                        _reloadAllRelated?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


    }
}
