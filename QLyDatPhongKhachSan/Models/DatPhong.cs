using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("DatPhong")]
    public class DatPhong
    {
        [Key]
        public int MaDatPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
        [Display(Name = "Khách hàng")]
        public int MaKhachHang { get; set; }

        [Display(Name = "Ngày đặt phòng")]
        [DataType(DataType.DateTime)]
        public DateTime NgayDat { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Ngày nhận phòng không được để trống")]
        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        public DateTime NgayNhan { get; set; }

        [Required(ErrorMessage = "Ngày trả phòng dự kiến không được để trống")]
        [Display(Name = "Ngày trả phòng dự kiến")]
        [DataType(DataType.Date)]
        public DateTime NgayTraDuKien { get; set; }

        [Required(ErrorMessage = "Số người không được để trống")]
        [Range(1, 100, ErrorMessage = "Số người phải lớn hơn 0")]
        [Display(Name = "Số người")]
        public int SoNguoi { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền cọc (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TienCoc { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng tiền (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TongTien { get; set; } = 0;

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái đặt phòng")]
        public string TrangThai { get; set; } = "ChoXuLy"; 
        // ChoXuLy (Chờ xử lý), DangXuLy (Đang xử lý / Tiếp nhận), DaXacNhan (Đã xác nhận), DangSuDung (Đã nhận phòng), HoanThanh (Đã trả phòng / Hoàn thành), DaHuy (Đã hủy)

        [Display(Name = "Ghi chú yêu cầu đặc biệt")]
        public string? GhiChu { get; set; }

        // Navigation
        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();
    }
}
