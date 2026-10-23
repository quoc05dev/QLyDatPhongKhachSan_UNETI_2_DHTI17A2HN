using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Repositories
{
    public class ChiTietDatPhongRepository : GenericRepository<ChiTietDatPhong>, IChiTietDatPhongRepository
    {
        private readonly AppDbContext _context;

        public ChiTietDatPhongRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<ChiTietDatPhong?> LayTheoMaAsync(int id) =>
            _context.ChiTietDatPhongs
                .Include(ct => ct.Phong)
                .Include(ct => ct.DatPhong)
                .FirstOrDefaultAsync(ct => ct.MaChiTiet == id);

        public async Task ThemAsync(ChiTietDatPhong chiTiet)
        {
            _context.Add(chiTiet);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(ChiTietDatPhong chiTiet)
        {
            _context.Update(chiTiet);
            await _context.SaveChangesAsync();
        }

        // ============ Thống kê doanh thu (Module 5 - Dashboard & báo cáo) ============

        public Task<bool> CoGiaoDichAsync() =>
            _context.ChiTietDatPhongs.AsNoTracking().AnyAsync();

        public Task<decimal> TongDoanhThuHieuLucAsync() =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(ct => ct.TrangThai != "DaHuy")
                .SumAsync(ct => ct.ThanhTien);

        public Task<decimal> DoanhThuThangHieuLucAsync(int nam, int thang) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(ct => ct.TrangThai != "DaHuy" && ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang)
                .SumAsync(ct => ct.ThanhTien);

        public Task<decimal> DoanhThuKyHieuLucAsync(int nam, int thang) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(ct => ct.TrangThai != "DaHuy" && ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang)
                .SumAsync(ct => ct.ThanhTien);

        public Task<int> DemLuotDatHieuLucTrongKyAsync(int nam, int thang) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .CountAsync(ct => ct.TrangThai != "DaHuy" && ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang);

        public Task<int> DemLuotHuyTrongKyAsync(int nam, int thang) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .CountAsync(ct => ct.TrangThai == "DaHuy" && ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang);

        public async Task<List<int>> LayDanhSachNamCoGiaoDichAsync()
        {
            return await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Select(ct => ct.NgayNhan.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
        }

        public async Task<List<DoanhThuThangItem>> ThongKeDoanhThuTheoThangAsync(int nam)
        {
            return await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(ct => ct.NgayNhan.Year == nam && ct.TrangThai != "DaHuy")
                .GroupBy(ct => ct.NgayNhan.Month)
                .Select(g => new DoanhThuThangItem
                {
                    Nam = nam,
                    Thang = g.Key,
                    DoanhThu = g.Sum(ct => ct.ThanhTien),
                    SoLuotDat = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToListAsync();
        }

        public async Task<List<LoaiPhongThongKeItem>> ThongKeDoanhThuTheoLoaiTrongKyAsync(int nam, int thang)
        {
            var chiTiets = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(ct => ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang && ct.TrangThai != "DaHuy")
                .ToListAsync();

            return chiTiets
                .Where(ct => ct.Phong != null && ct.Phong.LoaiPhong != null)
                .GroupBy(ct => ct.Phong!.LoaiPhong!.TenLoai)
                .Select(g => new LoaiPhongThongKeItem
                {
                    TenLoai = g.Key,
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();
        }

        public async Task<List<PhongThongKeItem>> ThongKePhongDatNhieuNhatAsync(int soPhongToiDa)
        {
            var chiTiets = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(ct => ct.TrangThai != "DaHuy")
                .ToListAsync();

            return chiTiets
                .Where(ct => ct.Phong != null)
                .GroupBy(ct => new { ct.MaPhong, ct.Phong!.SoPhong, ct.Phong.LoaiPhong!.TenLoai })
                .Select(g => new PhongThongKeItem
                {
                    SoPhong = g.Key.SoPhong,
                    TenLoai = g.Key.TenLoai,
                    SoLuotDat = g.Count(),
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(p => p.SoLuotDat)
                .ThenByDescending(p => p.DoanhThu)
                .Take(soPhongToiDa)
                .ToList();
        }

        public async Task<List<PhongThongKeItem>> ThongKePhongDatNhieuNhatTrongKyAsync(int nam, int thang, int soPhongToiDa)
        {
            var chiTiets = await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(ct => ct.NgayNhan.Year == nam && ct.NgayNhan.Month == thang && ct.TrangThai != "DaHuy")
                .ToListAsync();

            return chiTiets
                .Where(ct => ct.Phong != null)
                .GroupBy(ct => new { ct.MaPhong, ct.Phong!.SoPhong, ct.Phong.LoaiPhong!.TenLoai })
                .Select(g => new PhongThongKeItem
                {
                    SoPhong = g.Key.SoPhong,
                    TenLoai = g.Key.TenLoai,
                    SoLuotDat = g.Count(),
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(p => p.SoLuotDat)
                .ThenByDescending(p => p.DoanhThu)
                .Take(soPhongToiDa)
                .ToList();
        }

        // ============ Nghiệp vụ đặt phòng (Module 3 & 4) ============

        public Task<bool> PhongDaDuocDatTrongKhoangAsync(int maPhong, DateTime ngayNhan, DateTime ngayTraDuKien) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.DatPhong)
                .AnyAsync(ct => ct.MaPhong == maPhong
                             && ct.DatPhong != null
                             && ct.DatPhong.TrangThai != "DaHuy"
                             && ct.DatPhong.TrangThai != "HoanThanh"
                             && ct.NgayNhan.Date < ngayTraDuKien.Date
                             && ct.NgayTra.Date > ngayNhan.Date);

        public Task<bool> PhongDaDuocDatTrongKhoangTheoDonAsync(int maPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(c => c.DatPhong)
                .AnyAsync(c => c.MaPhong == maPhong
                           && c.MaDatPhong != maDatPhong
                           && c.DatPhong!.TrangThai != "DaHuy"
                           && c.DatPhong!.TrangThai != "HoanThanh"
                           && ngayNhan < c.DatPhong!.NgayTraDuKien
                           && ngayTraDuKien > c.DatPhong!.NgayNhan);

        public Task<int> DemPhongDaDatTrongKhoangAsync(int maLoaiPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien) =>
            _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(c => c.Phong!.MaLoaiPhong == maLoaiPhong
                         && c.MaDatPhong != maDatPhong
                         && c.DatPhong!.TrangThai != "DaHuy"
                         && c.DatPhong!.TrangThai != "HoanThanh"
                         && ngayNhan < c.DatPhong!.NgayTraDuKien
                         && ngayTraDuKien > c.DatPhong!.NgayNhan)
                .Select(c => c.MaPhong)
                .Distinct()
                .CountAsync();

        public async Task<List<int>> LayDanhSachPhongDaDatTrongKhoangAsync(int maLoaiPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien)
        {
            return await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Where(c => c.Phong!.MaLoaiPhong == maLoaiPhong
                         && c.MaDatPhong != maDatPhong
                         && c.DatPhong!.TrangThai != "DaHuy"
                         && c.DatPhong!.TrangThai != "HoanThanh"
                         && ngayNhan < c.DatPhong!.NgayTraDuKien
                         && ngayTraDuKien > c.DatPhong!.NgayNhan)
                .Select(c => c.MaPhong)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<ChiTietDatPhong>> LayLichSuTheoKhachHangAsync(int maKhachHang)
        {
            return await _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                .Where(ct => ct.DatPhong != null
                         && ct.DatPhong.MaKhachHang == maKhachHang
                         && ct.DatPhong.TrangThai != "DaHuy"
                         && ct.Phong != null)
                .ToListAsync();
        }
    }
}
