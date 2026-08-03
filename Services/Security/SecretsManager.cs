using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace sara_coursework.Services.Security
{
    public class AppSecrets
    {
        public string EncryptionKey { get; set; } = null!;
        public string EncryptionIV { get; set; } = null!;
        public string DbConnectionString { get; set; } = null!;
    }

    public static class SecretsManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "sara_coursework");
        private static readonly string SecretsFilePath = Path.Combine(AppDataFolder, "secrets.json");

        public static AppSecrets Secrets { get; private set; } = null!;

        static SecretsManager()
        {
            LoadSecrets();
        }

        private static void LoadSecrets()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                if (File.Exists(SecretsFilePath))
                {
                    string json = File.ReadAllText(SecretsFilePath);
                    var encryptedSecrets = JsonSerializer.Deserialize<AppSecrets>(json);

                    if (encryptedSecrets != null)
                    {
                        Secrets = new AppSecrets
                        {
                            EncryptionKey = DecryptString(encryptedSecrets.EncryptionKey),
                            EncryptionIV = DecryptString(encryptedSecrets.EncryptionIV),
                            DbConnectionString = DecryptString(encryptedSecrets.DbConnectionString)
                        };
                        return;
                    }
                }

                InitializeDefaultSecrets();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при инициализации SecretsManager", ex);
            }
        }

        private static void InitializeDefaultSecrets()
        {
            // If no existing secrets.json, generate cryptographically secure random keys
            string key = ConfigurationManager.AppSettings["EncryptionKey"] ?? GenerateRandomBase64Key(32);
            string iv = ConfigurationManager.AppSettings["EncryptionIV"] ?? GenerateRandomBase64Key(16);
            string dbConn = ConfigurationManager.ConnectionStrings["AwardsDB"]?.ConnectionString
                ?? "Server=(localdb)\\mssqllocaldb;Database=AwardsDB;Trusted_Connection=True;MultipleActiveResultSets=true";

            Secrets = new AppSecrets
            {
                EncryptionKey = key,
                EncryptionIV = iv,
                DbConnectionString = dbConn
            };

            var encrypted = new AppSecrets
            {
                EncryptionKey = EncryptString(key),
                EncryptionIV = EncryptString(iv),
                DbConnectionString = EncryptString(dbConn)
            };

            string json = JsonSerializer.Serialize(encrypted, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SecretsFilePath, json);
        }

        private static string GenerateRandomBase64Key(int lengthInBytes)
        {
            byte[] keyBytes = new byte[lengthInBytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }

        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string DecryptString(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return string.Empty;

            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
