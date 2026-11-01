using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    /// <summary>
    /// Log hành vi người dùng + nguồn thông báo cho trang quản trị.
    /// LoaiHoatDong: DangNhap | XemPhong | DatPhong | DanhGia.
    /// DatPhong/DanhGia được hiển thị lên chuông báo của Admin/Nhân viên.
    /// </summary>
    [Table("HoatDong")]
    public class HoatDong
    {
        [Key]
        public int MaHoatDong { get; set; }

        [Display(Name = "Thời gian")]
        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [Display(Name = "Tài khoản")]
        public int? MaTaiKhoan { get; set; }

        [StringLength(100)]
        [Display(Name = "Người thực hiện")]
        public string? TenNguoiDung { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Loại hoạt động")]
        public string LoaiHoatDong { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; } = string.Empty;

        [Display(Name = "Mã liên quan (đơn đặt phòng)")]
        public int? MaLienQuan { get; set; }

        [Display(Name = "Đã đọc")]
        public bool DaDoc { get; set; }

        [Display(Name = "Là thông báo")]
        public bool LaThongBao { get; set; }
    }
}
