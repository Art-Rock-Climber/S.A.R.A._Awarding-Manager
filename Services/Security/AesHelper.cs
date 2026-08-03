using System;
using System.IO;
using System.Security.Cryptography;

namespace sara_coursework.Services.Security
{
    public static class AesHelper
    {
        private static readonly byte[] Key;
        private static readonly byte[] IV;

        static AesHelper()
        {
            try
            {
                string base64Key = SecretsManager.Secrets.EncryptionKey;
                if (string.IsNullOrEmpty(base64Key))
                    throw new Exception("Ключ шифрования не найден в SecretsManager");

                string base64IV = SecretsManager.Secrets.EncryptionIV;
                if (string.IsNullOrEmpty(base64IV))
                    throw new Exception("IV не найден в SecretsManager");

                Key = ValidateAndConvertKey(base64Key);
                IV = ValidateAndConvertKey(base64IV);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка инициализации шифрования", ex);
            }
        }

        private static byte[] ValidateAndConvertKey(string base64Key)
        {
            try
            {
                byte[] keyBytes = Convert.FromBase64String(base64Key.Trim());
                return keyBytes;
            }
            catch (FormatException)
            {
                throw new Exception("Некорректный формат ключа (требуется Base64)");
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка шифрования", ex);
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                string cleanCipher = cipherText
                    .Trim()
                    .Replace(" ", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

                if (cleanCipher.Length % 4 != 0)
                    cleanCipher = cleanCipher.PadRight(
                        cleanCipher.Length + (4 - cleanCipher.Length % 4) % 4, '=');

                byte[] buffer = Convert.FromBase64String(cleanCipher);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(buffer))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (FormatException)
            {
                throw new Exception("Некорректные данные для дешифрования (ожидается Base64)");
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка дешифрования", ex);
            }
        }
    }
}
