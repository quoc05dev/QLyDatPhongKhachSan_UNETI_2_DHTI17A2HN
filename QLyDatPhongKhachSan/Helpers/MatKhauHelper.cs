// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Băm mật khẩu bằng PBKDF2-HMAC-SHA256,
// không lưu mật khẩu dạng rõ trong database.

using System.Security.Cryptography;

namespace QLyDatPhongKhachSan.Helpers
{
    public static class MatKhauHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        // Định dạng lưu trữ: PBKDF2$<số lần lặp>$<salt base64>$<hash base64>
        public static string Hash(string? matKhau)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                matKhau ?? string.Empty,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string? matKhauHash, string? matKhau)
        {
            if (string.IsNullOrEmpty(matKhauHash) || matKhau == null)
            {
                return false;
            }

            // Mật khẩu cũ lưu dạng rõ, vẫn cho đăng nhập được để không khóa tài khoản
            if (!matKhauHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                return matKhauHash == matKhau;
            }

            var parts = matKhauHash.Split('$');
            if (parts.Length != 4
                || !int.TryParse(parts[1], out int iterations)
                || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                matKhau,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}