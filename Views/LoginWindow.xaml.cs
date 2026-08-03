using sara_coursework.data;
using sara_coursework.models;
using sara_coursework.Services.Security;
using System;
using System.Linq;
using System.Windows;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public User? CurrentUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new AppDbContext();
                var user = context.Users.FirstOrDefault(u => u.Username == username);

                if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt))
                {
                    MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentUser = user;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            bool? registerResult = registerWindow.ShowDialog();

            if (registerResult == true)
            {
                DialogResult = null; // Сигнализирует о перенаправлении
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
