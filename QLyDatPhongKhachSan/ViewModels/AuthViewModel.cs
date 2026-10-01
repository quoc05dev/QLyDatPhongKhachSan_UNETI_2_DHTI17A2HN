// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - ViewModels cho Đăng nhập, Đăng ký, Quản lý tài khoản.

using System.ComponentModel.DataAnnotations;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 - 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD/Hộ chiếu không được để trống")]
        [StringLength(20, ErrorMessage = "CCCD không quá 20 ký tự")]
        [Display(Name = "Số CCCD")]
        public string CCCD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhau { get; set; } = string.Empty;
    }

    public class TaiKhoanListViewModel
    {
        public List<Models.TaiKhoan> Items { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;
        public bool? StatusFilter { get; set; }

        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; } = 0;

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
