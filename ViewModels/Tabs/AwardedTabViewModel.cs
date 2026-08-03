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
    public class AwardedTabViewModel : ViewModelBase
    {
        private readonly IAwardedRepository _awardedRepo;
        private readonly IAwardAssignmentRepository _assignmentRepo;
        private readonly ILogRepository _logRepo;
        private readonly Func<User?> _getCurrentUser;
        private readonly Action _reloadLogs;
        private readonly Action _reloadAwardings;

        private AwardedViewModel? _selectedAwarded;
        private string _searchText = string.Empty;
        private List<Awarded> _allAwarded = new();

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
                    ApplyFilterAndPagination();
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
                    ApplyFilterAndPagination();
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
                    ApplyFilterAndPagination();
                }
            }
        }

        public AwardedViewModel? SelectedAwarded
        {
            get => _selectedAwarded;
            set => SetProperty(ref _selectedAwarded, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public AwardedTabViewModel(
            IAwardedRepository awardedRepo,
            IAwardAssignmentRepository assignmentRepo,
            ILogRepository logRepo,
            Func<User?> getCurrentUser,
            Action reloadLogs,
            Action reloadAwardings)
        {
            _awardedRepo = awardedRepo;
            _assignmentRepo = assignmentRepo;
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;
            _reloadLogs = reloadLogs;
            _reloadAwardings = reloadAwardings;

            AddCommand = new RelayCommand(o => ExecuteAdd(), o => CanCRUD());
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => CanCRUD());
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => CanCRUD());
            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });
        }

        private bool CanCRUD() => _getCurrentUser() != null && _getCurrentUser()?.Role != UserRole.ReadOnly;

        public ObservableCollection<AwardedViewModel> AwardedList { get; } = new();

        public void LoadData()
        {
            try
            {
                _allAwarded = _awardedRepo.GetAwarded();
                ApplyFilterAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки награждаемых: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndPagination()
        {
            var filtered = _allAwarded.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                filtered = filtered.Where(a =>
                {
                    if (a is Citizen citizen)
                    {
                        return (citizen.LastName != null && citizen.LastName.ToLower().Contains(query)) ||
                               (citizen.FirstName != null && citizen.FirstName.ToLower().Contains(query)) ||
                               (citizen.MiddleName != null && citizen.MiddleName.ToLower().Contains(query)) ||
                               (citizen.Position != null && citizen.Position.ToLower().Contains(query));
                    }
                    else if (a is Collective collective)
                    {
                        return collective.CollectiveName != null && collective.CollectiveName.ToLower().Contains(query);
                    }
                    return false;
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

            AwardedList.Clear();
            foreach (var a in paged)
            {
                AwardedList.Add(new AwardedViewModel
                {
                    Id = a.Id,
                    AwardedType = a is Citizen ? "Гражданин" : "Коллектив",
                    DisplayName = a is Citizen citizen ? citizen.ToString() : ((Collective)a).CollectiveName
                });
            }
        }

        private void ExecuteAdd()
        {
            var addAwardedWindow = new EditAwardedWindow(_awardedRepo, _assignmentRepo);
            if (addAwardedWindow.ShowDialog() == true)
            {
                LoadData();
                _logRepo.LogAction("Info", "AddAwarded",
                    $"Пользователь {_getCurrentUser()?.Username ?? "System"} добавил награждаемого.",
                    _getCurrentUser()?.Username ?? "System");
                _reloadLogs();
                _reloadAwardings();
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedAwarded != null)
            {
                var editAwardedWindow = new EditAwardedWindow(_awardedRepo, _assignmentRepo, SelectedAwarded.Id);
                if (editAwardedWindow.ShowDialog() == true)
                {
                    LoadData();
                    _logRepo.LogAction("Info", "EditAwarded",
                        $"Пользователь {_getCurrentUser()?.Username ?? "System"} изменил награждаемого (ID: {SelectedAwarded.Id}).",
                        _getCurrentUser()?.Username ?? "System");
                    _reloadLogs();
                    _reloadAwardings();
                }
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedAwarded != null)
            {
                var res = MessageBox.Show("Удалить выбранного награждаемого?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        int idToDelete = SelectedAwarded.Id;
                        _awardedRepo.DeleteAwarded(idToDelete);
                        LoadData();
                        _logRepo.LogAction("Info", "DeleteAwarded",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} удалил награждаемого (ID: {idToDelete}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
                        _reloadAwardings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}\n\nВозможно, существуют связанные записи.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


    }
}
