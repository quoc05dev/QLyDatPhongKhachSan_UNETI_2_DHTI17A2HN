using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface IDichVuRepository : IGenericRepository<DichVu>
    {
        Task<List<DichVu>> TimKiem(string? searchTerm);
        Task<DichVu?> LayTheoMaAsync(int id);
        Task<bool> TenDaTonTaiAsync(string tenDichVu, int? boQuaMa = null);
        Task<bool> TonTaiAsync(int id);
        Task ThemAsync(DichVu dichVu);
        Task CapNhatAsync(DichVu dichVu);
        Task XoaAsync(DichVu dichVu);
    }
}
