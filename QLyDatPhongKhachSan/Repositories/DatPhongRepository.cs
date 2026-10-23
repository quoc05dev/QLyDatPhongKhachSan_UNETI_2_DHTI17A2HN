using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Repositories
{
    public class DatPhongRepository : GenericRepository<DatPhong>, IDatPhongRepository
    {
        private readonly AppDbContext _context;

        public DatPhongRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<DatPhong?> LayTheoMaVoiChiTietAsync(int id) =>
            _context.DatPhongs
                .Include(dp => dp.ChiTietDatPhongs)
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == id);

        public async Task CapNhatAsync(DatPhong datPhong)
        {
            _context.Update(datPhong);
            await _context.SaveChangesAsync();
        }

        // ============ Thống kê đơn đặt phòng (Module 5 - Dashboard & báo cáo) ============

        public Task<int> DemDatPhongAsync() =>
            _context.DatPhongs.AsNoTracking().CountAsync();

        public Task<int> DemDatPhongTheoTrangThaiAsync(string trangThai) =>
            _context.DatPhongs.AsNoTracking().CountAsync(dp => dp.TrangThai == trangThai);

        public async Task<List<DatPhong>> LayDatPhongGanNhatAsync(int soLuong)
        {
            return await _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .OrderByDescending(dp => dp.NgayDat)
                .Take(soLuong)
                .ToListAsync();
        }

        public async Task<List<DatPhong>> LayDatPhongNhanPhongTrongKyAsync(int nam, int thang)
        {
            return await _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Where(dp => dp.NgayNhan.Year == nam && dp.NgayNhan.Month == thang)
                .OrderByDescending(dp => dp.NgayNhan)
                .ToListAsync();
        }

        public async Task<List<DoanhThuThangItem>> ThongKeLuotDatTheoThangAsync(int nam)
        {
            return await _context.DatPhongs
                .AsNoTracking()
                .Where(dp => dp.NgayDat.Year == nam)
                .GroupBy(dp => dp.NgayDat.Month)
                .Select(g => new DoanhThuThangItem
                {
                    Nam = nam,
                    Thang = g.Key,
                    SoLuotDat = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToListAsync();
        }

        // ============ Nghiệp vụ đặt phòng (Module 3 & 4) ============

        public async Task ThemAsync(DatPhong datPhong)
        {
            _context.Add(datPhong);
            await _context.SaveChangesAsync();
        }

        public Task<bool> TonTaiAsync(int maDatPhong) =>
            _context.DatPhongs.AsNoTracking().AnyAsync(d => d.MaDatPhong == maDatPhong);

        public Task<DatPhong?> LayChiTietCuaKhachHangAsync(int maDatPhong, int maKhachHang) =>
            _context.DatPhongs
                .Include(d => d.KhachHang)
                    .ThenInclude(kh => kh!.TaiKhoan)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong && d.MaKhachHang == maKhachHang);

        public Task<DatPhong?> LayDeHuyCuaKhachHangAsync(int maDatPhong, int maKhachHang) =>
            _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong && d.MaKhachHang == maKhachHang);

        public Task<DatPhong?> LayTheoMaVoiKhachHangVaChiTietAsync(int maDatPhong) =>
            _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

        public Task<DatPhong?> LayTheoMaVoiChiTietVaPhongAsync(int maDatPhong) =>
            _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

        public Task<DatPhong?> LayTheoMaVoiChiTietVaLoaiPhongAsync(int maDatPhong) =>
            _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

        public async Task<(List<DatPhong> DanhSach, int TongBanGhi, int TrangHienTai, int TongSoTrang)> TimKiemDatPhongAsync(string? tuKhoa, string? trangThai, int? maLoaiPhong, DateTime? tuNgay, DateTime? denNgay, string? sapXep, int trang, int pageSize)
        {
            var query = _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                // 8.1: Tim kiem theo nguoi dung (ten, sdt), doi tuong chinh (so phong) va ma don
                query = query.Where(d => d.KhachHang!.HoTen.ToLower().Contains(tuKhoa) ||
                                         d.KhachHang!.SoDienThoai.Contains(tuKhoa) ||
                                         d.MaDatPhong.ToString().Contains(tuKhoa) ||
                                         d.ChiTietDatPhongs.Any(ct => ct.Phong!.SoPhong.ToLower().Contains(tuKhoa)));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            if (maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(d => d.ChiTietDatPhongs.Any(ct => ct.Phong!.MaLoaiPhong == maLoaiPhong.Value));
            }

            // Lọc khoảng ngày đúng biên — dùng .Date và .AddDays(1) để bao trọn ngày cuối
            if (tuNgay.HasValue) query = query.Where(d => d.NgayNhan >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(d => d.NgayNhan < denNgay.Value.Date.AddDays(1));

            // 8.1: Sắp xếp theo ngày hoặc tên
            query = sapXep switch
            {
                "ngaynhan_asc" => query.OrderBy(d => d.NgayNhan),
                "ngaynhan_desc" => query.OrderByDescending(d => d.NgayNhan),
                "tenkhach_asc" => query.OrderBy(d => d.KhachHang!.HoTen),
                "tenkhach_desc" => query.OrderByDescending(d => d.KhachHang!.HoTen),
                "ngaydat_asc" => query.OrderBy(d => d.NgayDat),
                _ => query.OrderByDescending(d => d.NgayDat)
            };

            int tongBanGhi = await query.CountAsync();

            // Đảm bảo trang không vượt quá tổng số trang
            int tongSoTrang = (int)Math.Ceiling((double)tongBanGhi / pageSize);
            if (tongSoTrang > 0 && trang > tongSoTrang) trang = tongSoTrang;

            var danhSach = await query
                .Skip((trang - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (danhSach, tongBanGhi, trang, tongSoTrang);
        }

        public async Task<List<DatPhongItemViewModel>> LayLichSuCuaKhachHangAsync(int maKhachHang, string? trangThaiFilter, string sortOrder, int skip, int take)
        {
            var query = _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .AsNoTracking()
                .Where(d => d.MaKhachHang == maKhachHang);

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(trangThaiFilter))
            {
                query = query.Where(d => d.TrangThai == trangThaiFilter);
            }

            // Sắp xếp (Sorting - Mục 22 - De_15)
            query = sortOrder switch
            {
                "date_asc" => query.OrderBy(d => d.NgayDat),
                "price_desc" => query.OrderByDescending(d => d.TongTien),
                "price_asc" => query.OrderBy(d => d.TongTien),
                "checkin_asc" => query.OrderBy(d => d.NgayNhan),
                _ => query.OrderByDescending(d => d.NgayDat)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .Select(d => new DatPhongItemViewModel
                {
                    MaDatPhong = d.MaDatPhong,
                    MaPhong = d.ChiTietDatPhongs.Select(ct => (int?)ct.MaPhong).FirstOrDefault(),
                    SoPhong = d.ChiTietDatPhongs.Select(ct => ct.Phong != null ? ct.Phong.SoPhong : "").FirstOrDefault() ?? "Chưa gán",
                    TenLoaiPhong = d.ChiTietDatPhongs.Select(ct => ct.Phong != null && ct.Phong.LoaiPhong != null ? ct.Phong.LoaiPhong.TenLoai : "").FirstOrDefault() ?? "",
                    NgayDat = d.NgayDat,
                    NgayNhan = d.NgayNhan,
                    NgayTraDuKien = d.NgayTraDuKien,
                    SoNguoi = d.SoNguoi,
                    SoNgayO = (d.NgayTraDuKien.Date - d.NgayNhan.Date).Days < 1 ? 1 : (d.NgayTraDuKien.Date - d.NgayNhan.Date).Days,
                    TienCoc = d.TienCoc,
                    TongTien = d.TongTien,
                    TrangThai = d.TrangThai,
                    GhiChu = d.GhiChu
                })
                .ToListAsync();
        }

        public Task<int> DemLichSuCuaKhachHangAsync(int maKhachHang, string? trangThaiFilter)
        {
            var query = _context.DatPhongs
                .AsNoTracking()
                .Where(d => d.MaKhachHang == maKhachHang);

            if (!string.IsNullOrEmpty(trangThaiFilter))
            {
                query = query.Where(d => d.TrangThai == trangThaiFilter);
            }

            return query.CountAsync();
        }

        public async Task<Dictionary<string, int>> ThongKeTrangThaiCuaKhachHangAsync(int maKhachHang)
        {
            var thongKe = await _context.DatPhongs
                .AsNoTracking()
                .Where(d => d.MaKhachHang == maKhachHang)
                .GroupBy(d => d.TrangThai)
                .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                .ToListAsync();

            return thongKe.ToDictionary(x => x.TrangThai, x => x.SoLuong);
        }
    }
}
