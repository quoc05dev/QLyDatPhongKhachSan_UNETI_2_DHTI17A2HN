// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: ViewModel trang chủ công khai theo bản thiết kế
// DesignProjectWithSuggestions (hero, hạng phòng nổi bật, đặt phòng).

namespace QLyDatPhongKhachSan.ViewModels
{
    public class HomeViewModel
    {
        public List<HomePhongCard> Phongs { get; set; } = new List<HomePhongCard>();
        public List<string> LoaiPhongs { get; set; } = new List<string>();
        public string? LoaiPhongFilter { get; set; }
        public bool IsLoggedIn { get; set; }
        public string? VaiTro { get; set; }
        public string? HoTen { get; set; }
    }

    public class HomePhongCard
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public string TenLoai { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}