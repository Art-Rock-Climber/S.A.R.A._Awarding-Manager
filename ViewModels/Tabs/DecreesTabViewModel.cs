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
    public class DecreesTabViewModel : ViewModelBase
    {
        private readonly IDecreeRepository _decreeRepo;
        private readonly IAwardReasonRepository _reasonRepo;
        private readonly ILogRepository _logRepo;
        private readonly Func<User?> _getCurrentUser;
        private readonly Action _reloadLogs;
        private readonly Action _reloadAwardings;

        private Decree? _selectedDecree;
        private string _searchText = string.Empty;
        private List<Decree> _allDecrees = new();

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

        public Decree? SelectedDecree
        {
            get => _selectedDecree;
            set => SetProperty(ref _selectedDecree, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public DecreesTabViewModel(
            IDecreeRepository decreeRepo,
            IAwardReasonRepository reasonRepo,
            ILogRepository logRepo,
            Func<User?> getCurrentUser,
            Action reloadLogs,
            Action reloadAwardings)
        {
            _decreeRepo = decreeRepo;
            _reasonRepo = reasonRepo;
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

        public ObservableCollection<Decree> Decrees { get; } = new();

        public void LoadData()
        {
            try
            {
                _allDecrees = _decreeRepo.GetDecrees();
                ApplyFilterAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки постановлений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndPagination()
        {
            var filtered = _allDecrees.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                filtered = filtered.Where(d =>
                    (d.Number != null && d.Number.ToLower().Contains(query)) ||
                    (d.Date.ToString("dd.MM.yyyy").Contains(query)) ||
                    (d.AwardReason != null && d.AwardReason.ReasonName != null && d.AwardReason.ReasonName.ToLower().Contains(query))
                );
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

            Decrees.Clear();
            foreach (var d in paged)
            {
                Decrees.Add(d);
            }
        }

        private void ExecuteAdd()
        {
            var addDecreeWindow = new EditDecreeWindow(_decreeRepo, _reasonRepo);
            if (addDecreeWindow.ShowDialog() == true)
            {
                LoadData();
                _logRepo.LogAction("Info", "AddDecree",
                    $"Пользователь {_getCurrentUser()?.Username ?? "System"} добавил постановление.",
                    _getCurrentUser()?.Username ?? "System");
                _reloadLogs();
                _reloadAwardings();
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedDecree != null)
            {
                var editDecreeWindow = new EditDecreeWindow(_decreeRepo, _reasonRepo, SelectedDecree.Id);
                if (editDecreeWindow.ShowDialog() == true)
                {
                    LoadData();
                    _logRepo.LogAction("Info", "EditDecree",
                        $"Пользователь {_getCurrentUser()?.Username ?? "System"} изменил постановление (ID: {SelectedDecree.Id}).",
                        _getCurrentUser()?.Username ?? "System");
                    _reloadLogs();
                    _reloadAwardings();
                }
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedDecree != null)
            {
                var res = MessageBox.Show("Удалить выбранное постановление?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        int idToDelete = SelectedDecree.Id;
                        _decreeRepo.DeleteDecree(idToDelete);
                        LoadData();
                        _logRepo.LogAction("Info", "DeleteDecree",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} удалил постановление (ID: {idToDelete}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
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
