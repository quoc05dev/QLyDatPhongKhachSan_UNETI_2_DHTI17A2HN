// ==========================================
// Họ và tên: Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - ViewModel Quản lý danh sách khách hàng (CRUD, Tìm kiếm, Lọc, Phân trang)
// ==========================================

using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class KhachHangListViewModel
    {
        public List<KhachHang> Items { get; set; } = new List<KhachHang>();

        // Bộ lọc & Tìm kiếm
        public string? SearchTerm { get; set; }
        public string? GioiTinhFilter { get; set; }
        public bool? StatusFilter { get; set; }
        public string? CoTaiKhoanFilter { get; set; } // "co" (Có tài khoản), "khong" (Vãng lai)
        public string SortOrder { get; set; } = "id_desc";

        // Thống kê nhanh toàn bộ CSDL
        public int TongKhachHang { get; set; }
        public int SoKhachHoatDong { get; set; }
        public int SoKhachBiKhoa { get; set; }
        public int SoKhachCoTaiKhoan { get; set; }
        public int SoKhachVangLai { get; set; }

        // Phân trang LINQ EF Core
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
