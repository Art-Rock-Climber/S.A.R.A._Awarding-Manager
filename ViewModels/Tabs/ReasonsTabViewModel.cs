using sara_coursework.models;
using sara_coursework.Services.Repositories;
using sara_coursework.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels.Tabs
{
    public class ReasonsTabViewModel : ViewModelBase
    {
        private readonly IAwardReasonRepository _reasonRepo;
        private readonly ILogRepository _logRepo;
        private readonly Func<User?> _getCurrentUser;
        private readonly Action _reloadLogs;
        private readonly Action _reloadDecrees;

        private AwardReason? _selectedReason;
        private string _searchText = string.Empty;
        private List<AwardReason> _allReasons = new();

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

        public AwardReason? SelectedReason
        {
            get => _selectedReason;
            set => SetProperty(ref _selectedReason, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ReasonsTabViewModel(
            IAwardReasonRepository reasonRepo,
            ILogRepository logRepo,
            Func<User?> getCurrentUser,
            Action reloadLogs,
            Action reloadDecrees)
        {
            _reasonRepo = reasonRepo;
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;
            _reloadLogs = reloadLogs;
            _reloadDecrees = reloadDecrees;

            AddCommand = new RelayCommand(o => ExecuteAdd(), o => CanCRUD());
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => CanCRUD());
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => CanCRUD());
            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });
        }

        private bool CanCRUD() => _getCurrentUser() != null && _getCurrentUser()?.Role != UserRole.ReadOnly;

        public ObservableCollection<AwardReason> Reasons { get; } = new();

        public void LoadData()
        {
            try
            {
                _allReasons = _reasonRepo.GetAwardReasons();
                ApplyFilterAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки оснований: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndPagination()
        {
            var filtered = _allReasons.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                filtered = filtered.Where(r => r.ReasonName != null && r.ReasonName.ToLower().Contains(query));
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

            Reasons.Clear();
            foreach (var r in paged)
            {
                Reasons.Add(r);
            }
        }

        private void ExecuteAdd()
        {
            var addReasonDialog = new SimpleEditWindow("Добавление основания", "Название основания:");
            if (addReasonDialog.ShowDialog() == true)
            {
                try
                {
                    _reasonRepo.SaveAwardReason(new AwardReason { ReasonName = addReasonDialog.ResultText });
                    LoadData();
                    _logRepo.LogAction("Info", "AddReason",
                        $"Пользователь {_getCurrentUser()?.Username ?? "System"} добавил основание {addReasonDialog.ResultText}.",
                        _getCurrentUser()?.Username ?? "System");
                    _reloadLogs();
                    _reloadDecrees();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления основания: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedReason != null)
            {
                var editReasonDialog = new SimpleEditWindow("Редактирование основания", "Название основания:", SelectedReason.ReasonName);
                if (editReasonDialog.ShowDialog() == true)
                {
                    try
                    {
                        SelectedReason.ReasonName = editReasonDialog.ResultText;
                        _reasonRepo.SaveAwardReason(SelectedReason);
                        LoadData();
                        _logRepo.LogAction("Info", "EditReason",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} изменил основание (ID: {SelectedReason.Id}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
                        _reloadDecrees();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка редактирования основания: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedReason != null)
            {
                var res = MessageBox.Show("Удалить выбранное основание?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        int idToDelete = SelectedReason.Id;
                        _reasonRepo.DeleteAwardReason(idToDelete);
                        LoadData();
                        _logRepo.LogAction("Info", "DeleteReason",
                            $"Пользователь {_getCurrentUser()?.Username ?? "System"} удалил основание (ID: {idToDelete}).",
                            _getCurrentUser()?.Username ?? "System");
                        _reloadLogs();
                        _reloadDecrees();
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
