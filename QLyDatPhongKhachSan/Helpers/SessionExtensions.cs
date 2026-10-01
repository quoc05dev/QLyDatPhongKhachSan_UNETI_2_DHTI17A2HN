// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Helper quản lý đọc/ghi thông tin người dùng trong Session.

namespace QLyDatPhongKhachSan.Helpers
{
    public static class SessionExtensions
    {
        public const string KeyMaTaiKhoan = "MaTaiKhoan";
        public const string KeyTenDangNhap = "TenDangNhap";
        public const string KeyHoTen = "HoTen";
        public const string KeyVaiTro = "VaiTro";

        public static void SetUserSession(this ISession session, int maTaiKhoan, string tenDangNhap, string hoTen, string vaiTro)
        {
            session.SetInt32(KeyMaTaiKhoan, maTaiKhoan);
            session.SetString(KeyTenDangNhap, tenDangNhap ?? string.Empty);
            session.SetString(KeyHoTen, hoTen ?? string.Empty);
            session.SetString(KeyVaiTro, vaiTro ?? string.Empty);
        }

        public static int? GetMaTaiKhoan(this ISession session)
        {
            return session.GetInt32(KeyMaTaiKhoan);
        }

        public static string GetTenDangNhap(this ISession session)
        {
            return session.GetString(KeyTenDangNhap) ?? string.Empty;
        }

        public static string GetHoTen(this ISession session)
        {
            return session.GetString(KeyHoTen) ?? string.Empty;
        }

        public static string GetVaiTro(this ISession session)
        {
            return session.GetString(KeyVaiTro) ?? string.Empty;
        }

        public static bool IsLoggedIn(this ISession session)
        {
            return session.GetInt32(KeyMaTaiKhoan).HasValue;
        }

        public static void ClearUserSession(this ISession session)
        {
            session.Clear();
        }
    }
}
