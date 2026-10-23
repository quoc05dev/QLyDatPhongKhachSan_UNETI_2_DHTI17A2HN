using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface ITaiKhoanRepository : IGenericRepository<TaiKhoan>
    {
        Task<TaiKhoan?> LayTheoMaAsync(int id);
        Task<TaiKhoan?> LayTheoTenDangNhapAsync(string tenDangNhap);
        Task<bool> TenDangNhapDaTonTaiAsync(string tenDangNhap, int? boQuaMa = null);
        Task<bool> EmailDaTonTaiAsync(string? email, int? boQuaMa = null);
        Task<bool> TonTaiAsync(int id);
        Task<List<TaiKhoan>> TimKiemAsync(string? searchTerm, string? roleFilter, bool? statusFilter);
        Task<int> DemAsync(string? searchTerm, string? roleFilter, bool? statusFilter);
        Task<List<TaiKhoan>> LayPhanTrangAsync(string? searchTerm, string? roleFilter, bool? statusFilter, int skip, int take);
        Task<TaiKhoan?> LayTheoMaCoKhachHangAsync(int id);
        Task<TaiKhoan?> LayTheoMaCoKhachHangTheoDoiAsync(int id);
        Task<List<TaiKhoan>> LayTatCaChuaLienKetKhachHangAsync();
        Task<List<TaiKhoan>> LayTatCaCoTheLienKetAsync(int? currentMaTaiKhoan = null);
        Task ThemAsync(TaiKhoan taiKhoan);
        Task CapNhatAsync(TaiKhoan taiKhoan);
        Task XoaAsync(TaiKhoan taiKhoan);
    }
}