using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("ChiTietDatPhong")]
    public class ChiTietDatPhong
    {
        [Key]
        public int MaChiTiet { get; set; }

        [Required]
        [Display(Name = "Mã đặt phòng")]
        public int MaDatPhong { get; set; }

        [Required]
        [Display(Name = "Phòng")]
        public int MaPhong { get; set; }

        [Display(Name = "Ngày nhận thực tế")]
        [DataType(DataType.Date)]
        public DateTime NgayNhan { get; set; }

        [Display(Name = "Ngày trả thực tế")]
        [DataType(DataType.Date)]
        public DateTime NgayTra { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Đơn giá (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal DonGia { get; set; }

        [Display(Name = "Số ngày ở")]
        public int SoNgay { get; set; } = 1;

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Thành tiền (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal ThanhTien { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái chi tiết")]
        public string TrangThai { get; set; } = "ChoNhan"; // ChoNhan, DaNhan, DaTra, DaHuy

        // Navigation
        [ForeignKey("MaDatPhong")]
        public virtual DatPhong? DatPhong { get; set; }

        [ForeignKey("MaPhong")]
        public virtual Phong? Phong { get; set; }
    }
}
