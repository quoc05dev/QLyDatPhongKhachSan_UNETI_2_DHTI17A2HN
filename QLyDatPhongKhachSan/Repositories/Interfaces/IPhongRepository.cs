using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface IPhongRepository : IGenericRepository<Phong>
    {
        Task<List<Phong>> TimKiemVaLocAsync(string? searchTerm, int? maLoaiPhong, string? trangThai, string sortOrder);
        Task<List<Phong>> LayDanhSachPhongKhongBaoTriAsync();
        Task<List<Phong>> LayPhongHienThiTrangChuAsync();
        Task<Phong?> LayTheoMaAsync(int id);
        Task<Phong?> LayTheoMaCoLoaiPhongAsync(int id);
        Task<Phong?> LayTheoMaTheoDoiAsync(int id);
        Task<bool> SoPhongDaTonTaiAsync(string soPhong, int? boQuaMa = null);
        Task<bool> TonTaiPhongThuocLoaiAsync(int maLoaiPhong);
        Task<IQueryable<Phong>> QueryCoLoaiPhongNoTracking();
        Task<List<Phong>> TimKiemVaLocFullAsync(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, DateTime? ngayNhan, DateTime? ngayTra, string? sortOrder, int skip, int take);
        Task<int> DemTimKiemVaLocFullAsync(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, DateTime? ngayNhan, DateTime? ngayTra);
        Task<bool> CoLichSuDatPhongAsync(int maPhong);
        Task<List<Phong>> LayDanhSachPhongTrongAsync();
        Task<List<Phong>> LayTatCaAsync();
        Task<List<Phong>> TimKiemVaLocPhongDatPhongAsync(string? keyword, int? maLoaiPhong);
        Task<List<Phong>> TimKiemVaLocPhongDatPhongAsync(int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, string? keyword);
        Task<int> DemPhongKhongBaoTriTheoLoaiAsync(int maLoaiPhong);
        Task<List<Phong>> LayPhongTrongCungLoaiAsync(int maLoaiPhong);
        Task<List<Phong>> LayPhongTrongCungLoaiAsync(int maLoaiPhong, List<int>? phongDaChonTrongDon, List<int>? phongBanIds);
        Task ThemAsync(Phong phong);
        Task CapNhatAsync(Phong phong);
        Task XoaAsync(Phong phong);
    }
}