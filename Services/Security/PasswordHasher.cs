using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace sara_coursework.Services.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 32; // 256 бит
        private const int Iterations = 100000;
        private const int KeySize = 256 / 8; // 256 бит

        public static (string Hash, string Salt) CreateHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");

            byte[] salt = GenerateSalt();
            byte[] hash = GenerateHash(password, salt);

            return (
                Hash: Convert.ToBase64String(hash),
                Salt: Convert.ToBase64String(salt)
            );
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(storedHash) ||
                string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(storedSalt);
                byte[] hash = Convert.FromBase64String(storedHash);
                byte[] newHash = GenerateHash(password, salt);

                return CryptographicOperations.FixedTimeEquals(hash, newHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] GenerateHash(string password, byte[] salt)
        {
            return KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: Iterations,
                numBytesRequested: KeySize);
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }
    }
}
