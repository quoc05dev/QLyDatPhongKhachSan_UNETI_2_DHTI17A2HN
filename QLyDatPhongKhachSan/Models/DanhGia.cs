using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLyDatPhongKhachSan.Models
{
    /// <summary>
    /// Đánh giá của khách hàng cho một đơn đã hoàn thành (mỗi đơn 1 đánh giá).
    /// TinhCam + GoiYPhanHoi do AI (rule-based) phân tích cảm xúc và soạn nháp trả lời.
    /// </summary>
    [Table("DanhGia")]
    public class DanhGia
    {
        [Key]
        public int MaDanhGia { get; set; }

        [Display(Name = "Đơn đặt phòng")]
        public int MaDatPhong { get; set; }

        [StringLength(200)]
        [Display(Name = "Khách hàng")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        [Display(Name = "Số sao")]
        public int SoSao { get; set; } = 5;

        [StringLength(1000)]
        [Display(Name = "Nội dung đánh giá")]
        public string BinhLuan { get; set; } = string.Empty;

        [Display(Name = "Ngày đánh giá")]
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;

        [StringLength(20)]
        [Display(Name = "Cảm xúc")]
        public string TinhCam { get; set; } = "TrungLap";

        [Display(Name = "Điểm cảm xúc")]
        public double DiemCamXuc { get; set; }

        [StringLength(600)]
        [Display(Name = "Gợi ý trả lời (AI)")]
        public string? GoiYPhanHoi { get; set; }

        [StringLength(600)]
        [Display(Name = "Phản hồi chính thức")]
        public string? PhanHoi { get; set; }

        [Display(Name = "Đã phản hồi")]
        public bool DaPhanHoi { get; set; }

        [Display(Name = "Đơn đặt phòng")]
        public virtual DatPhong? DatPhong { get; set; }
    }
}
