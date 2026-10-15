// ==========================================
// Họ và tên: Lê Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Helper dùng chung để lấy ảnh minh họa loại phòng (Module 3)
// ==========================================

using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Helpers
{
    /// <summary>
    /// Lấy ảnh đại diện cho loại phòng: ưu tiên cột LoaiPhong.HinhAnh, nếu chưa có thì dùng ảnh mẫu theo tên loại.
    /// </summary>
    public static class AnhPhongHelper
    {
        private static readonly Dictionary<string, string> AnhMauTheoLoai =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Phòng Tiêu Chuẩn"] = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1100&q=86",
                ["Phòng Nâng Cao"] = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1100&q=86",
                ["Phòng Cao Cấp"] = "https://images.unsplash.com/photo-1668277155881-9d3bccb1683a?auto=format&fit=crop&w=1100&q=86",
                ["Phòng Suite"] = "https://images.unsplash.com/photo-1556426983-5705abbfcad6?auto=format&fit=crop&w=1100&q=86",
                ["Phòng Hai Phòng"] = "https://images.unsplash.com/photo-1776876648949-63ccabf63b10?auto=format&fit=crop&w=1100&q=86",
            };

        private const string AnhMacDinh = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1100&q=86";

        public static string LayAnhMau(string? tenLoai)
        {
            if (!string.IsNullOrWhiteSpace(tenLoai) && AnhMauTheoLoai.TryGetValue(tenLoai, out var anh))
            {
                return anh;
            }
            return AnhMacDinh;
        }

        public static string LayAnh(LoaiPhong? loaiPhong)
        {
            if (loaiPhong == null) return AnhMacDinh;
            return string.IsNullOrWhiteSpace(loaiPhong.HinhAnh) ? LayAnhMau(loaiPhong.TenLoai) : loaiPhong.HinhAnh!;
        }

        private static readonly string[] DinhDangAnhChoPhep = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };

        /// <summary>
        /// Lấy danh sách ảnh gallery của một phòng dựa trên thư mục quy ước:
        /// wwwroot/images/phong/phong-{MaPhong}/1.jpg, 2.jpg, ...
        /// Nếu thư mục trống hoặc không tồn tại thì fallback về ảnh đại diện của loại phòng.
        /// </summary>
        public static List<string> LayDanhSachAnh(Phong? phong, string? webRootPath)
        {
            var ketQua = new List<string>();

            if (phong != null && !string.IsNullOrWhiteSpace(webRootPath))
            {
                var thuMuc = Path.Combine(webRootPath, "images", "phong", $"phong-{phong.MaPhong}");
                if (Directory.Exists(thuMuc))
                {
                    var files = Directory.GetFiles(thuMuc)
                        .Where(f => DinhDangAnhChoPhep.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f =>
                        {
                            var ten = Path.GetFileNameWithoutExtension(f);
                            return int.TryParse(ten, out var so) ? so : int.MaxValue;
                        })
                        .ThenBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var f in files)
                    {
                        ketQua.Add($"/images/phong/phong-{phong.MaPhong}/{Path.GetFileName(f)}");
                    }
                }
            }

            if (ketQua.Count == 0)
            {
                ketQua.Add(LayAnh(phong?.LoaiPhong));
            }

            return ketQua;
        }

        public static string LayMoTa(LoaiPhong? loaiPhong)
        {
            if (loaiPhong != null && !string.IsNullOrWhiteSpace(loaiPhong.MoTa))
            {
                return loaiPhong.MoTa!;
            }
            return "Không gian nghỉ dưỡng tinh tuyển với đầy đủ tiện nghi, được hoàn thiện để mang đến sự thư thái trọn vẹn cho kỳ nghỉ của bạn.";
        }
    }
}
