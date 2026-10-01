// ==========================================
// Họ và tên: Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - Quản lý khách hàng, hồ sơ cá nhân
// Chức năng: Khách hàng chỉ xem/sửa hồ sơ của chính mình
// ==========================================

using System.ComponentModel.DataAnnotations;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class HoSoCaNhanViewModel
    {
        public int MaKhachHang { get; set; }

        public int? MaTaiKhoan { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = "KhachHang";

        [Display(Name = "Ngày tạo tài khoản")]
        public DateTime? NgayTaoTaiKhoan { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        [StringLength(10)]
        public string? GioiTinh { get; set; } = "Nam";

        [Required(ErrorMessage = "Số CCCD/Hộ chiếu không được để trống")]
        [StringLength(20, MinimumLength = 9, ErrorMessage = "CCCD/Hộ chiếu từ 9 đến 20 ký tự")]
        [Display(Name = "Số CCCD / Hộ chiếu")]
        public string CCCD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20, MinimumLength = 9, ErrorMessage = "Số điện thoại từ 9 đến 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email không quá 100 ký tự")]
        [Display(Name = "Địa chỉ Email")]
        public string? Email { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không quá 255 ký tự")]
        [Display(Name = "Địa chỉ liên hệ")]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Quốc tịch không được để trống")]
        [StringLength(50, ErrorMessage = "Quốc tịch không quá 50 ký tự")]
        [Display(Name = "Quốc tịch")]
        public string QuocTich { get; set; } = "Việt Nam";

        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
        [Display(Name = "Ghi chú cá nhân / Yêu cầu đặc biệt")]
        public string? GhiChu { get; set; }
    }
}
