using sara_coursework.data;
using sara_coursework.models;
using sara_coursework.Services.Security;
using System;
using System.Linq;
using System.Windows;

namespace sara_coursework.Views
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            using var context = new AppDbContext();

            if (context.Users.Any(u => u.Username == username))
            {
                MessageBox.Show("Пользователь с таким логином уже существует");
                return;
            }

            var (hash, salt) = PasswordHasher.CreateHash(password);

            var newUser = new User
            {
                Username = username,
                PasswordHash = hash,
                Salt = salt,
                Role = UserRole.User,
                CreatedAt = DateTime.Now
            };

            try
            {
                context.Users.Add(newUser);
                context.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
