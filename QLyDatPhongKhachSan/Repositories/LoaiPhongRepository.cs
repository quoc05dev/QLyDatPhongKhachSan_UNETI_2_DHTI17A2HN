using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Repositories
{
    public class LoaiPhongRepository : GenericRepository<LoaiPhong>, ILoaiPhongRepository
    {
        private readonly AppDbContext _context;

        public LoaiPhongRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<LoaiPhong>> TimKiemVaLocAsync(string? searchTerm, bool? statusFilter, string sortOrder)
        {
            var query = _context.LoaiPhongs.Include(l => l.Phongs).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.TenLoai.ToLower().Contains(term));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(l => l.TrangThai == statusFilter.Value);
            }

            sortOrder ??= "name_asc";
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(l => l.DonGiaNgay),
                "price_desc" => query.OrderByDescending(l => l.DonGiaNgay),
                "capacity_desc" => query.OrderByDescending(l => l.SoNguoiToiDa),
                _ => query.OrderBy(l => l.TenLoai)
            };

            return await query.ToListAsync();
        }

        public async Task<List<LoaiPhong>> LayTatCaHoatDongAsync() =>
            await _context.LoaiPhongs
                .AsNoTracking()
                .Where(lp => lp.TrangThai)
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();

        public Task<LoaiPhong?> LayTheoMaVoiPhongAsync(int id) =>
            _context.LoaiPhongs
                .Include(l => l.Phongs)
                .FirstOrDefaultAsync(l => l.MaLoaiPhong == id);

        public Task<LoaiPhong?> LayTheoMaAsync(int id) =>
            _context.LoaiPhongs.AsNoTracking().FirstOrDefaultAsync(l => l.MaLoaiPhong == id);

        public Task<bool> TenDaTonTaiAsync(string tenLoai, int? boQuaMa = null) =>
            _context.LoaiPhongs.AnyAsync(l => l.TenLoai == tenLoai && (boQuaMa == null || l.MaLoaiPhong != boQuaMa));

        public async Task ThemAsync(LoaiPhong loaiPhong)
        {
            _context.Add(loaiPhong);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(LoaiPhong loaiPhong)
        {
            _context.Update(loaiPhong);
            await _context.SaveChangesAsync();
        }

        public async Task XoaAsync(LoaiPhong loaiPhong)
        {
            _context.Remove(loaiPhong);
            await _context.SaveChangesAsync();
        }

        // ============ Thống kê (Module 5 - Dashboard & báo cáo doanh thu) ============

        public async Task<List<LoaiPhong>> LayTatCaAsync()
        {
            return await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToListAsync();
        }
    }
}
