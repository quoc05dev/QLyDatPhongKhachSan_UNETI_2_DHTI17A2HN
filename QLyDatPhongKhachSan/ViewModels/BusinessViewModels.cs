using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class DatPhongViewModel
    {
        public int? MaPhong { get; set; }
        public Phong? Phong { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ và tên khách hàng")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "CCCD không được để trống")]
        [Display(Name = "CCCD / Hộ chiếu")]
        public string CCCD { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime NgayNhan { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng dự kiến")]
        public DateTime NgayTraDuKien { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng nhập số người")]
        [Range(1, 20, ErrorMessage = "Số người từ 1 đến 20")]
        [Display(Name = "Số lượng khách")]
        public int SoNguoi { get; set; } = 1;

        [Display(Name = "Ghi chú / Yêu cầu đặc biệt")]
        public string? GhiChu { get; set; }

        [Display(Name = "Tiền cọc")]
        public decimal TienCoc { get; set; } = 0;
    }

    public class TiepNhanViewModel
    {
        public List<DatPhong> DanhSachDatPhong { get; set; } = new List<DatPhong>();
        public string? SearchTerm { get; set; }
        public string? TrangThaiFilter { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
    }

    public class DashboardViewModel
    {
        public int TongLoaiPhong { get; set; }
        public int TongSoPhong { get; set; }
        public int SoPhongTrong { get; set; }
        public int SoPhongDangSuDung { get; set; }
        public int SoPhongBaoTri { get; set; }
        public int TongKhachHang { get; set; }
        public int TongDonDatPhong { get; set; }
        public int DonChoXuLy { get; set; }
        public int DonDangXuLy { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonDaHuy { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal DoanhThuThangNay { get; set; }

        public List<PhongThongKeItem> PhongDatNhieuNhat { get; set; } = new List<PhongThongKeItem>();
        public List<DoanhThuThangItem> DoanhThuTheoThang { get; set; } = new List<DoanhThuThangItem>();
        public List<LoaiPhongThongKeItem> TyLePhongTheoLoai { get; set; } = new List<LoaiPhongThongKeItem>();
    }

    public class PhongThongKeItem
    {
        public string SoPhong { get; set; } = string.Empty;
        public string TenLoai { get; set; } = string.Empty;
        public int SoLuotDat { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class DoanhThuThangItem
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoLuotDat { get; set; }
    }

    public class LoaiPhongThongKeItem
    {
        public string TenLoai { get; set; } = string.Empty;
        public int SoLuongPhong { get; set; }
        public double TyLe { get; set; }

        // Chỉ dùng ở báo cáo doanh thu theo loại phòng (mục 9.4).
        public decimal DoanhThu { get; set; }
    }
}
