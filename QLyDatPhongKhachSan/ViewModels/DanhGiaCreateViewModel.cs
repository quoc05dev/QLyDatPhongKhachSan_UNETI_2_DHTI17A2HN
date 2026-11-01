using System.ComponentModel.DataAnnotations;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class DanhGiaCreateViewModel
    {
        public int MaDatPhong { get; set; }

        [Range(1, 5, ErrorMessage = "Bạn hãy chọn số sao (1-5).")]
        [Display(Name = "Đánh giá của bạn")]
        public int SoSao { get; set; } = 5;

        [Required(ErrorMessage = "Bạn hãy viết vài dòng cảm nhận.")]
        [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
        [Display(Name = "Cảm nhận của bạn")]
        public string BinhLuan { get; set; } = string.Empty;
    }
}
