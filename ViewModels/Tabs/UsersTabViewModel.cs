using sara_coursework.models;
using sara_coursework.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace sara_coursework.ViewModels.Tabs
{
    public class UsersTabViewModel : ViewModelBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ILogRepository? _logRepo;
        private readonly Func<User?>? _getCurrentUser;

        private string _searchText = string.Empty;
        private List<User> _allUsers = new();
        private UserViewModel? _selectedUser;

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
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

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

        public UserViewModel? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public ObservableCollection<UserViewModel> Users { get; } = new();

        public UsersTabViewModel(IUserRepository userRepo, ILogRepository? logRepo = null, Func<User?>? getCurrentUser = null)
        {
            _userRepo = userRepo;
            _logRepo = logRepo;
            _getCurrentUser = getCurrentUser;

            PreviousPageCommand = new RelayCommand(o => { if (HasPreviousPage) CurrentPage--; });
            NextPageCommand = new RelayCommand(o => { if (HasNextPage) CurrentPage++; });
            AddCommand = new RelayCommand(o => ExecuteAdd(), o => IsAdmin());
            EditCommand = new RelayCommand(o => ExecuteEdit(), o => IsAdmin());
            DeleteCommand = new RelayCommand(o => ExecuteDelete(), o => IsAdmin());
        }

        private bool IsAdmin() => _getCurrentUser == null || _getCurrentUser()?.Role == UserRole.Admin;

        private void ExecuteAdd()
        {
            var window = new sara_coursework.Views.EditUserWindow(_userRepo);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                LoadData();
                _logRepo?.LogAction("Info", "AddUser",
                    $"Создан новый пользователь.",
                    _getCurrentUser?.Invoke()?.Username ?? "System");
            }
        }

        private void ExecuteEdit()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new sara_coursework.Views.EditUserWindow(_userRepo, SelectedUser);
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog() == true)
            {
                LoadData();
                _logRepo?.LogAction("Info", "EditUser",
                    $"Изменен пользователь '{SelectedUser.UserName}' (ID: {SelectedUser.Id}).",
                    _getCurrentUser?.Invoke()?.Username ?? "System");
            }
        }

        private void ExecuteDelete()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentUser = _getCurrentUser?.Invoke();
            if (currentUser != null && currentUser.Id == SelectedUser.Id)
            {
                MessageBox.Show("Нельзя удалить собственного пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить пользователя '{SelectedUser.UserName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int idToDelete = SelectedUser.Id;
                    string nameToDelete = SelectedUser.UserName;
                    _userRepo.DeleteUser(idToDelete);
                    _logRepo?.LogAction("Info", "DeleteUser",
                        $"Удален пользователь '{nameToDelete}' (ID: {idToDelete}).",
                        currentUser?.Username ?? "System");
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LoadData()
        {
            try
            {
                _allUsers = _userRepo.GetUsers();
                ApplyFilterAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndPagination()
        {
            var filtered = _allUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                filtered = filtered.Where(u =>
                    (u.Username != null && u.Username.ToLower().Contains(query)) ||
                    (u.Role.ToString().ToLower().Contains(query))
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

            Users.Clear();
            foreach (var u in paged)
            {
                Users.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.Username,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                });
            }
        }
    }
}
