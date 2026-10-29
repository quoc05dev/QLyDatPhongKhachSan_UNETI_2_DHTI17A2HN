using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 - 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        // Không [Required] và không MinimumLength vì khi sửa tài khoản, để trống nghĩa là giữ mật khẩu cũ.
        // Ràng buộc bắt buộc 6 ký tự được kiểm tra thủ công trong Controller khi tạo mới
        // hoặc khi người dùng chủ động nhập mật khẩu mới.
        [StringLength(255, ErrorMessage = "Mật khẩu không quá 255 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = "KhachHang"; // Admin, NhanVien, KhachHang

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Định danh duy nhất của Google (claim "sub"). Chỉ lưu cho tài khoản
        // được tạo/liên kết qua Google OAuth.
        [StringLength(255)]
        public string? GoogleSubject { get; set; }

        // Đánh dấu tài khoản được tạo bằng Google. Tài khoản có mật khẩu cục bộ
        // sẽ không được tự động liên kết chỉ dựa vào email (chống pre-hijack).
        [Display(Name = "Đăng nhập bằng Google")]
        public bool LaTaiKhoanGoogle { get; set; }

        // Navigation
        public virtual KhachHang? KhachHang { get; set; }
    }
}
