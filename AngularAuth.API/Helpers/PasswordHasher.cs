using System.Security.Cryptography;
using System.Text;

namespace AngularAuth.API.Helpers
{
    public class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 10000;

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = key.GetBytes(HashSize);

            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string base64Hash)
        {
            byte[] hashBytes = Convert.FromBase64String(base64Hash);

            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] computedHash = key.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[SaltSize + i] != computedHash[i])
                    return false;
            }
            return true;
        }

        public static string CheckPasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Password cannot be empty.";

            var sb = new StringBuilder();

            if (password.Length < 8)
                sb.AppendLine("Minimum password length should be 8.");

            bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (!(hasLower && hasUpper && hasDigit))
                sb.AppendLine("Password should be alphanumeric (contain at least one uppercase letter, one lowercase letter, and one digit).");

            if (!hasSpecial)
                sb.AppendLine("Password should contain at least one special character.");

            return sb.ToString();
        }
    }
}
