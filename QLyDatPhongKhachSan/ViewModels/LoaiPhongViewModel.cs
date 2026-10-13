// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - ViewModel danh sách Loại phòng (tìm kiếm, lọc, sắp xếp, phân trang).

using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class LoaiPhongListViewModel
    {
        public List<LoaiPhong> Items { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public bool? StatusFilter { get; set; }
        public string SortOrder { get; set; } = "name_asc";

        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; } = 0;
    }
}
