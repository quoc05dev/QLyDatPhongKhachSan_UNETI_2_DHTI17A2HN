// Ho va ten: Dau Duc Luu
// Ma sinh vien: 23103100114
// Noi dung: ViewModel danh sach Phong - Search, Filter, Sort, Pagination
using Microsoft.AspNetCore.Mvc.Rendering;
using QLyDatPhongKhachSan.Models;
using System.ComponentModel.DataAnnotations;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class PhongListViewModel
    {
        public List<Phong> Phongs { get; set; } = new List<Phong>();

        // Search & Filter parameters
        public string? Keyword { get; set; }
        public int? MaLoaiPhong { get; set; }
        public int? Tang { get; set; }
        public string? TrangThai { get; set; }

        [Display(Name = "Giá tối thiểu")]
        public decimal? GiaMin { get; set; }

        [Display(Name = "Giá tối đa")]
        public decimal? GiaMax { get; set; }
        public DateTime? NgayNhan { get; set; }
        public DateTime? NgayTra { get; set; }

        // Sort parameter
        public string? SortOrder { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // Select lists
        public SelectList? LoaiPhongList { get; set; }
        public SelectList? TrangThaiList { get; set; }
    }
}
