using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        [StringLength(100, ErrorMessage = "Tên dịch vụ không vượt quá 100 ký tự")]
        [Display(Name = "Tên dịch vụ")]
        public string TenDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá dịch vụ không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá dịch vụ (VNĐ)")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Gia { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? MoTa { get; set; }

        [StringLength(255)]
        [Display(Name = "Hình ảnh minh họa")]
        public string? HinhAnh { get; set; }

        [Display(Name = "Trạng thái phục vụ")]
        public bool TrangThai { get; set; } = true;
    }
}
