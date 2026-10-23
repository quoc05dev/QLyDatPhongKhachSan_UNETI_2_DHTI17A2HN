using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface ILoaiPhongRepository : IGenericRepository<LoaiPhong>
    {
        Task<List<LoaiPhong>> TimKiemVaLocAsync(string? searchTerm, bool? statusFilter, string sortOrder);
        Task<List<LoaiPhong>> LayTatCaHoatDongAsync();
        Task<LoaiPhong?> LayTheoMaVoiPhongAsync(int id);
        Task<bool> TenDaTonTaiAsync(string tenLoai, int? boQuaMa = null);
        Task<LoaiPhong?> LayTheoMaAsync(int id);
        Task ThemAsync(LoaiPhong loaiPhong);
        Task CapNhatAsync(LoaiPhong loaiPhong);
        Task XoaAsync(LoaiPhong loaiPhong);

        // ============ Thống kê (Module 5 - Dashboard & báo cáo doanh thu) ============
        // Toàn bộ hạng phòng (kể cả hạng đã ngừng hoạt động), sắp xếp theo mã.
        Task<List<LoaiPhong>> LayTatCaAsync();
    }
}
