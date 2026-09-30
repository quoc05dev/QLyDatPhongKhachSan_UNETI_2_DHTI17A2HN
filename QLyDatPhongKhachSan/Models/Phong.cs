using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("Phong")]
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Số phòng không được để trống")]
        [StringLength(20, ErrorMessage = "Số phòng không vượt quá 20 ký tự")]
        [Display(Name = "Số phòng")]
        public string SoPhong { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        [Display(Name = "Loại phòng")]
        public int MaLoaiPhong { get; set; }

        [Required(ErrorMessage = "Tầng không được để trống")]
        [Range(1, 100, ErrorMessage = "Tầng phải từ 1 đến 100")]
        [Display(Name = "Tầng")]
        public int Tang { get; set; } = 1;

        [StringLength(100)]
        [Display(Name = "Hướng phòng (View)")]
        public string? HuongPhong { get; set; } // Hướng biển, Hướng phố, Hướng vườn...

        [Display(Name = "Tiện nghi")]
        public string? TienNghi { get; set; } // Smart TV, Ban công, Bồn tắm...

        [Required(ErrorMessage = "Đơn giá không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Đơn giá (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal DonGia { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Trong"; // Trong, DangXuLy, DaDat, DangSuDung, BaoTri

        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        [Display(Name = "Ngày bảo trì")]
        [DataType(DataType.Date)]
        public DateTime? NgayBaoTri { get; set; }

        // Bổ sung theo yêu cầu De_15.docx mục 6.1 (Entity Phong gồm tối thiểu 11 trường).
        // Thay đổi phối hợp giữa SV2 (23103100114 - quản lý Phong) và SV5 (23103100069).
        [StringLength(500)]
        [Display(Name = "Ghi chú bảo trì")]
        public string? GhiChuBaoTri { get; set; }

        // Navigation
        [ForeignKey("MaLoaiPhong")]
        public virtual LoaiPhong? LoaiPhong { get; set; }

        public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();
    }
}
