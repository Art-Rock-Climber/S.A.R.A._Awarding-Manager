using sara_coursework.data;
using sara_coursework.models;
using System;
using System.Linq;

namespace sara_coursework.Services.Security
{
    public static class DoubleEncryptionMigrator
    {
        public static void MigrateDoubleEncryption(AppDbContext context)
        {
            try
            {
                var citizens = context.Awarded.OfType<Citizen>().ToList();
                bool updated = false;

                foreach (var citizen in citizens)
                {
                    if (TryDecrypt(citizen.LastName, out string? plainLastName))
                    {
                        citizen.LastName = plainLastName!;
                        updated = true;
                    }

                    if (TryDecrypt(citizen.FirstName, out string? plainFirstName))
                    {
                        citizen.FirstName = plainFirstName!;
                        updated = true;
                    }

                    if (citizen.MiddleName != null && TryDecrypt(citizen.MiddleName, out string? plainMiddleName))
                    {
                        citizen.MiddleName = plainMiddleName;
                        updated = true;
                    }
                }

                if (updated)
                {
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка миграции двойного шифрования: {ex.Message}");
            }
        }

        private static bool TryDecrypt(string input, out string? decrypted)
        {
            decrypted = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            try
            {
                string clean = input.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "");
                if (clean.Length % 4 != 0) return false;
                Convert.FromBase64String(clean);

                decrypted = AesHelper.Decrypt(input);
                return !string.IsNullOrEmpty(decrypted);
            }
            catch
            {
                return false;
            }
        }
    }
}
