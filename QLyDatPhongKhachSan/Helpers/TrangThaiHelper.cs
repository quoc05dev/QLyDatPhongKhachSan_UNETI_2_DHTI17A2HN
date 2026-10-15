// ==========================================
// Họ và tên: Đậu Đức Lưu
// Mã sinh viên: 23103100114
// Nội dung thực hiện: Helper chuyển đổi mã trạng thái (không dấu) sang nhãn tiếng Việt có dấu để hiển thị.
// ==========================================

namespace QLyDatPhongKhachSan.Helpers
{
    /// <summary>
    /// Ánh xạ mã trạng thái lưu trong cơ sở dữ liệu (Trong, DangSuDung, ChoXuLy...)
    /// sang nhãn tiếng Việt có dấu dùng cho giao diện.
    /// </summary>
    public static class TrangThaiHelper
    {
        private static readonly Dictionary<string, string> NhanTrangThai =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Trạng thái phòng
                ["Trong"] = "Trống",
                ["DangSuDung"] = "Đang sử dụng",
                ["DangXuLy"] = "Đang xử lý",
                ["DaDat"] = "Đã đặt",
                ["BaoTri"] = "Bảo trì",

                // Trạng thái đơn đặt phòng
                ["ChoXuLy"] = "Chờ xử lý",
                ["DaXacNhan"] = "Đã xác nhận",
                ["HoanThanh"] = "Hoàn thành",
                ["DaHuy"] = "Đã hủy",

                // Trạng thái chi tiết đặt phòng
                ["ChoNhan"] = "Chờ nhận phòng",
                ["DaNhan"] = "Đã nhận phòng",
                ["DaTra"] = "Đã trả phòng"
            };

        /// <summary>
        /// Trả về nhãn tiếng Việt có dấu của trạng thái. Nếu không nhận diện được thì trả về nguyên bản.
        /// </summary>
        public static string HienThi(string? trangThai)
        {
            if (string.IsNullOrWhiteSpace(trangThai))
            {
                return "—";
            }

            return NhanTrangThai.TryGetValue(trangThai, out var nhan) ? nhan : trangThai;
        }
    }
}
