using sara_coursework.models;
using sara_coursework.Services.Security;
using sara_coursework.Services.Repositories;
using sara_coursework.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for EditUserWindow.xaml
    /// </summary>
    public partial class EditUserWindow : Window
    {
        private readonly IUserRepository _userRepo;
        private readonly UserViewModel? _editingUserVm;

        public EditUserWindow(IUserRepository userRepo, UserViewModel? userToEdit = null)
        {
            InitializeComponent();
            _userRepo = userRepo;
            _editingUserVm = userToEdit;

            txtUsername.TextChanged += (s, e) => txtUsername.ClearValue(BorderBrushProperty);
            txtPassword.PasswordChanged += (s, e) => txtPassword.ClearValue(BorderBrushProperty);
            txtConfirmPassword.PasswordChanged += (s, e) => txtConfirmPassword.ClearValue(BorderBrushProperty);
            cmbRole.SelectionChanged += (s, e) => cmbRole.ClearValue(BorderBrushProperty);

            if (_editingUserVm != null)
            {
                Title = "Редактирование пользователя";
                txtUsername.Text = _editingUserVm.UserName;
                lblPassword.Content = "Новый пароль (необязательно)";
                lblConfirmPassword.Content = "Подтверждение нового пароля";

                switch (_editingUserVm.Role)
                {
                    case UserRole.Admin: cmbRole.SelectedIndex = 0; break;
                    case UserRole.User: cmbRole.SelectedIndex = 1; break;
                    case UserRole.ReadOnly: cmbRole.SelectedIndex = 2; break;
                    default: cmbRole.SelectedIndex = 1; break;
                }
            }
            else
            {
                Title = "Добавление пользователя";
                cmbRole.SelectedIndex = 1; // По умолчанию Обычный пользователь
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;
                UserRole selectedRole = cmbRole.SelectedIndex switch
                {
                    0 => UserRole.Admin,
                    1 => UserRole.User,
                    2 => UserRole.ReadOnly,
                    _ => UserRole.User
                };

                var allUsers = _userRepo.GetUsers();

                if (_editingUserVm == null)
                {
                    // Создание нового пользователя
                    if (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var (hash, salt) = PasswordHasher.CreateHash(password);
                    var newUser = new User
                    {
                        Username = username,
                        PasswordHash = hash,
                        Salt = salt,
                        Role = selectedRole,
                        CreatedAt = DateTime.Now
                    };
                    _userRepo.SaveUser(newUser);
                }
                else
                {
                    // Редактирование существующего пользователя
                    if (allUsers.Any(u => u.Id != _editingUserVm.Id && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var userToUpdate = allUsers.FirstOrDefault(u => u.Id == _editingUserVm.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.Username = username;
                        userToUpdate.Role = selectedRole;

                        if (!string.IsNullOrEmpty(password))
                        {
                            var (hash, salt) = PasswordHasher.CreateHash(password);
                            userToUpdate.PasswordHash = hash;
                            userToUpdate.Salt = salt;
                        }

                        _userRepo.SaveUser(userToUpdate);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                txtUsername.BorderBrush = Brushes.Red;
                isValid = false;
            }

            if (_editingUserVm == null && string.IsNullOrEmpty(txtPassword.Password))
            {
                txtPassword.BorderBrush = Brushes.Red;
                isValid = false;
            }

            if (!string.IsNullOrEmpty(txtPassword.Password) && txtPassword.Password != txtConfirmPassword.Password)
            {
                txtConfirmPassword.BorderBrush = Brushes.Red;
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbRole.SelectedItem == null)
            {
                cmbRole.BorderBrush = Brushes.Red;
                isValid = false;
            }

            if (!isValid)
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (подсвечены красным).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return isValid;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
