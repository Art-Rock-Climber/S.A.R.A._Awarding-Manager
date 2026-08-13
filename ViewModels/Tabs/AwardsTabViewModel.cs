using sara_coursework.models;
using sara_coursework.Services.Repositories;
using sara_coursework.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels.Tabs
{
    public class AwardsTabViewModel : ViewModelBase
    {
        private readonly IAwardRepository _awardRepo;
        private readonly ILogRepository _logRepo;
        private readonly Func<User?> _getCurrentUser;
        private readonly Action _reloadLogs;
        private readonly Action _reloadFilters;
        private readonly Action _reloadAwardings;

        private Award? _selectedAward;
        private string _searchText = string.Empty;
        private List<Award> _allAwards = new();

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

        public Award? SelectedAward
        {
            get => _selectedAward;
            set => SetProperty(ref _selectedAward, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public AwardsTabViewModel(
            IAwardRepository awardRepo,
            ILogRepository logRepo,
            Func<User?> getCurrentUser,
            Action reloadLogs,
            Action reloadFilters,
            Action reloadAwardings)
        {
            _awardRepo = awardRepo;
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;
            _reloadLogs = reloadLogs;
            _reloadFilters = reloadFilters;
            _reloadAwardings = reloadAwardings;

            AddCommand = new RelayCommand(o => ExecuteAdd(), o => CanCRUD());
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => CanCRUD());
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => CanCRUD());
            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });
        }

        private bool CanCRUD() => _getCurrentUser() != null && _getCurrentUser()?.Role != UserRole.ReadOnly;

        public ObservableCollection<Award> Awards { get; } = new();

        public void LoadData()
        {
            try
            {
                _allAwards = _awardRepo.GetAwards();
                ApplyFilterAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки наград: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndPagination()
        {
            var filtered = _allAwards.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                filtered = filtered.Where(a => a.AwardName != null && a.AwardName.ToLower().Contains(query));
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

            Awards.Clear();
            foreach (var a in paged)
            {
                Awards.Add(a);
            }
        }

        private void ExecuteAdd()
        {
            var addAwardDialog = new SimpleEditWindow("Добавление награды", "Название награды:");
            if (addAwardDialog.ShowDialog() == true)
            {
                try
                {
                    _awardRepo.SaveAward(new Award { AwardName = addAwardDialog.ResultText });
                    LoadData();
                    _logRepo.LogAction("Info", "AddAward",
                        $"Пользователь {_getCurrentUser()?.Username ?? "System"} добавил награду {addAwardDialog.ResultText}.",
                        _getCurrentUser()?.Username ?? "System");
                    _reloadLogs();
                    _reloadFilters();
                    _reloadAwardings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления награды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedAward != null)
            {
                int editedId = SelectedAward.Id;
                var editAwardDialog = new SimpleEditWindow("Редактирование награды", "Название награды:", SelectedAward.AwardName);
                if (editAwardDialog.ShowDialog() == true)
                {
                    try
                    {
                        SelectedAward.AwardName = editAwardDialog.ResultText;
                        _awardRepo.SaveAward(SelectedAward);
                        _logRepo.LogAction("Info", "EditAward",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} изменил награду (ID: {editedId}).",
                            _getCurrentUser()?.Username ?? "System");
                        LoadData();
                        _reloadLogs();
                        _reloadFilters();
                        _reloadAwardings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка редактирования награды: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedAward != null)
            {
                var res = MessageBox.Show("Удалить выбранную награду?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        int idToDelete = SelectedAward.Id;
                        _awardRepo.DeleteAward(idToDelete);
                        LoadData();
                        _logRepo.LogAction("Info", "DeleteAward",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} удалил награду (ID: {idToDelete}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
                        _reloadFilters();
                        _reloadAwardings();
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
