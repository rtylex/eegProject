using System;
using System.Security.Cryptography;
using System.Text;

namespace eegProject.Security
{
    internal static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            var salt = Convert.ToBase64String(saltBytes);
            var hash = ComputeSha256(password, salt);
            return string.Concat(salt, ":", hash);
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = parts[0];
            var expectedHash = parts[1];
            var actualHash = ComputeSha256(password, salt);
            return SlowEquals(expectedHash, actualHash);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            return Verify(password, storedHash);
        }

        private static string ComputeSha256(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                var salted = salt + password;
                var bytes = Encoding.UTF8.GetBytes(salted);
                var hashBytes = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            var diff = (uint)a.Length ^ (uint)b.Length;
            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }
    }
}
