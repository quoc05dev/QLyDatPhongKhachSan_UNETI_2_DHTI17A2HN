using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface IKhachHangRepository : IGenericRepository<KhachHang>
    {
        Task<KhachHang?> LayTheoMaTaiKhoanAsync(int maTaiKhoan);
        Task<KhachHang?> LayTheoMaTaiKhoanCoDatPhongAsync(int maTaiKhoan);
        Task<KhachHang?> LayTheoMaAsync(int maKhachHang);
        Task<KhachHang?> LayTheoMaCoTaiKhoanAsync(int maKhachHang);
        Task<KhachHang?> LayTheoMaCoTaiKhoanVaDatPhongFullAsync(int maKhachHang);
        Task<KhachHang?> LayTheoMaTaiKhoanCoTaiKhoanAsync(int maTaiKhoan);
        Task<List<KhachHang>> TimKiemVaLocPhanTrangAsync(string? searchTerm, string? gioiTinhFilter, bool? statusFilter, string? coTaiKhoanFilter, string sortOrder, int skip, int take);
        Task<int> DemTimKiemVaLocAsync(string? searchTerm, string? gioiTinhFilter, bool? statusFilter, string? coTaiKhoanFilter);
        Task<int> DemTatCaAsync();
        Task<int> DemTheoTrangThaiAsync(bool trangThai);
        Task<int> DemCoTaiKhoanAsync(bool coTaiKhoan);
        Task<bool> CCCDDaTonTaiAsync(string cccd, int? boQuaMa = null);
        Task<bool> EmailDaTonTaiAsync(string? email, int? boQuaMa = null);
        Task<int> DemKhachHangAsync();
        Task ThemAsync(KhachHang khachHang);
        Task CapNhatAsync(KhachHang khachHang);
        Task XoaAsync(KhachHang khachHang);
    }
}