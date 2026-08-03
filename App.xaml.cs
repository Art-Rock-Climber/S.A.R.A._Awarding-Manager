using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.Views;
using sara_coursework.models;
using sara_coursework.Services.Security;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace sara_coursework
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("--migrate"))
            {
                ApplyDatabaseMigrations();
                Shutdown();
                return;
            }

            InitializeDatabase();

            var loginWindow = new LoginWindow();
            bool? authResult = loginWindow.ShowDialog();

            if (authResult == true)
            {
                var currentUser = loginWindow.CurrentUser;
                var mainWindow = new MainWindow(currentUser);

                mainWindow.Closed += (s, args) => Shutdown(); // Закрытие приложения при закрытии главного окна
                mainWindow.Show();

                // Явно активируем окно
                mainWindow.Activate();
            }
            else if (authResult == null)
            {
                // Пользователь выбрал регистрацию
                var registerWindow = new RegisterWindow();
                if (registerWindow.ShowDialog() == true)
                {
                    var newLoginWindow = new LoginWindow();
                    if (newLoginWindow.ShowDialog() == true)
                    {
                        var newMainWindow = new MainWindow(newLoginWindow.CurrentUser);

                        newMainWindow.Closed += (s, args) => Shutdown(); // Закрытие приложения при закрытии главного окна
                        newMainWindow.Show();

                        // Явно активируем окно
                        newMainWindow.Activate();

                    }
                    else
                    {
                        Shutdown();
                    }
                }
            }
            else
            {
                // Пользователь отменил вход
                Shutdown();
            }
        }

        private void InitializeDatabase()
        {
            using (var context = new AppDbContext())
            {
                if (!context.Users.Any())
                {
                    // Создаем администратора по умолчанию
                    var (hash, salt) = PasswordHasher.CreateHash("admin123");

                    context.Users.Add(new User
                    {
                        Username = "admin",
                        PasswordHash = hash,
                        Salt = salt,
                        Role = UserRole.Admin,
                        CreatedAt = DateTime.UtcNow
                    });

                    context.SaveChanges();
                }

                // Выполняем миграцию двойного шифрования для старых данных
                DoubleEncryptionMigrator.MigrateDoubleEncryption(context);
            }
        }

        private void ApplyDatabaseMigrations()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Database.Migrate();
                    File.WriteAllText("migration.log", "Миграции успешно применены");
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("migration_error.log", $"Ошибка миграции: {ex}");
                throw; // Для отладки в установщике
            }
        }
    }

}
