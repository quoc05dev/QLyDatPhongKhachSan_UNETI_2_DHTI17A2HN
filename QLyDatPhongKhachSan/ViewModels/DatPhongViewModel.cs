// ==========================================
// Họ và tên: Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - ViewModels phục vụ đặt phòng, theo dõi lịch sử và hủy đặt phòng
// ==========================================

using System.ComponentModel.DataAnnotations;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.ViewModels
{
    /// <summary>
    /// ViewModel phục vụ form tạo đơn đặt phòng mới (Book)
    /// </summary>
    public class DatPhongCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng cần đặt")]
        [Display(Name = "Mã phòng")]
        public int MaPhong { get; set; }

        [Display(Name = "Số phòng")]
        public string SoPhong { get; set; } = string.Empty;

        [Display(Name = "Loại phòng")]
        public string TenLoaiPhong { get; set; } = string.Empty;

        [Display(Name = "Đơn giá ngày")]
        public decimal DonGia { get; set; }

        [Display(Name = "Sức chứa tối đa")]
        public int SoNguoiToiDa { get; set; } = 2;

        public string? TienNghi { get; set; }
        public string? HuongPhong { get; set; }
        public int Tang { get; set; }

        // Thông tin khách hàng đặt phòng (Tự động nạp từ tài khoản đang đăng nhập)
        [Required(ErrorMessage = "Họ và tên người đặt không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên người đặt")]
        public string HoTenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại liên hệ")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "CCCD/Hộ chiếu không được để trống")]
        [StringLength(20)]
        [Display(Name = "Số CCCD / Hộ chiếu")]
        public string CCCD { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Thông tin thời gian và số lượng khách
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime NgayNhan { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng dự kiến")]
        public DateTime NgayTraDuKien { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng nhập số người")]
        [Range(1, 20, ErrorMessage = "Số lượng khách từ 1 đến 20")]
        [Display(Name = "Số lượng khách")]
        public int SoNguoi { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc không hợp lệ")]
        [Display(Name = "Tiền cọc (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TienCoc { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
        [Display(Name = "Ghi chú / Yêu cầu đặc biệt")]
        public string? GhiChu { get; set; }

        // Tính toán dự kiến
        public int SoNgayO
        {
            get
            {
                int days = (NgayTraDuKien.Date - NgayNhan.Date).Days;
                return days < 1 ? 1 : days;
            }
        }

        public decimal TongTienDuKien => SoNgayO * DonGia;
    }

    /// <summary>
    /// ViewModel hiển thị một dòng đơn đặt phòng trong danh sách lịch sử
    /// </summary>
    public class DatPhongItemViewModel
    {
        public int MaDatPhong { get; set; }
        public int? MaPhong { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public string TenLoaiPhong { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTraDuKien { get; set; }
        public int SoNguoi { get; set; }
        public int SoNgayO { get; set; }
        public decimal TienCoc { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = "ChoXuLy";
        public string? GhiChu { get; set; }

        /// <summary>
        /// Cho phép hủy khi đơn ở trạng thái 'ChoXuLy' hoặc 'DangXuLy'
        /// </summary>
        public bool CoTheHuy => TrangThai == "ChoXuLy" || TrangThai == "DangXuLy";
    }

    /// <summary>
    /// ViewModel cho trang danh sách lịch sử đặt phòng (/DatPhong/Index) kèm phân trang và sắp xếp
    /// </summary>
    public class DatPhongLichSuViewModel
    {
        public List<DatPhongItemViewModel> DanhSachDon { get; set; } = new List<DatPhongItemViewModel>();
        public string? TrangThaiFilter { get; set; }
        public string? SortOrder { get; set; } = "date_desc";

        // Thống kê nhanh theo trạng thái
        public int TongDon { get; set; }
        public int DonChoXuLy { get; set; }
        public int DonDaXacNhan { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonDaHuy { get; set; }

        // Phân trang (Mục 22 - De_15)
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// ViewModel chi tiết đơn đặt phòng (/DatPhong/Details/{id})
    /// </summary>
    public class DatPhongChiTietViewModel
    {
        public DatPhong DatPhong { get; set; } = null!;
        public ChiTietDatPhong? ChiTiet { get; set; }
        public Phong? Phong { get; set; }
        public KhachHang? KhachHang { get; set; }
        public int SoNgayO { get; set; }
        public decimal ConLaiPhaiThanhToan => DatPhong != null ? Math.Max(0, DatPhong.TongTien - DatPhong.TienCoc) : 0;
        public bool CoTheHuy => DatPhong != null && (DatPhong.TrangThai == "ChoXuLy" || DatPhong.TrangThai == "DangXuLy");
    }
}
