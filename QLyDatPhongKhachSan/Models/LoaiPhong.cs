using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("LoaiPhong")]
    public class LoaiPhong
    {
        [Key]
        public int MaLoaiPhong { get; set; }

        [Required(ErrorMessage = "Tên loại phòng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên loại phòng không vượt quá 100 ký tự")]
        [Display(Name = "Tên loại phòng")]
        public string TenLoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số người tối đa không được để trống")]
        [Range(1, 20, ErrorMessage = "Số người tối đa phải từ 1 đến 20")]
        [Display(Name = "Số người tối đa")]
        public int SoNguoiToiDa { get; set; } = 2;

        [Required(ErrorMessage = "Đơn giá ngày không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá ngày phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Đơn giá theo ngày (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal DonGiaNgay { get; set; }

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [StringLength(255)]
        [Display(Name = "Hình ảnh đại diện")]
        public string? HinhAnh { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool TrangThai { get; set; } = true;

        // Navigation
        public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
    }
}
