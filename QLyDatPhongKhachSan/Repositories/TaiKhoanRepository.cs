using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Repositories
{
    public class TaiKhoanRepository : GenericRepository<TaiKhoan>, ITaiKhoanRepository
    {
        private readonly AppDbContext _context;

        public TaiKhoanRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<TaiKhoan>> TimKiemAsync(string? searchTerm, string? roleFilter, bool? statusFilter)
        {
            var query = _context.TaiKhoans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(term) ||
                                         t.HoTen.ToLower().Contains(term) ||
                                         (t.Email != null && t.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(t => t.VaiTro == roleFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.TrangThai == statusFilter.Value);
            }

            return await query.OrderByDescending(t => t.MaTaiKhoan).ToListAsync();
        }

        public async Task<int> DemAsync(string? searchTerm, string? roleFilter, bool? statusFilter)
        {
            var query = _context.TaiKhoans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(term) ||
                                         t.HoTen.ToLower().Contains(term) ||
                                         (t.Email != null && t.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(t => t.VaiTro == roleFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.TrangThai == statusFilter.Value);
            }

            return await query.CountAsync();
        }

        public async Task<List<TaiKhoan>> LayPhanTrangAsync(string? searchTerm, string? roleFilter, bool? statusFilter, int skip, int take)
        {
            var query = _context.TaiKhoans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(term) ||
                                         t.HoTen.ToLower().Contains(term) ||
                                         (t.Email != null && t.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(t => t.VaiTro == roleFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.TrangThai == statusFilter.Value);
            }

            query = query.OrderByDescending(t => t.MaTaiKhoan);
            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public Task<TaiKhoan?> LayTheoMaAsync(int id) =>
            _context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(t => t.MaTaiKhoan == id);

        public Task<TaiKhoan?> LayTheoTenDangNhapAsync(string tenDangNhap) =>
            _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

        public Task<TaiKhoan?> LayTheoEmailAsync(string email) =>
            _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email == email);

        public Task<TaiKhoan?> LayTheoGoogleSubjectAsync(string googleSubject) =>
            _context.TaiKhoans.FirstOrDefaultAsync(t => t.GoogleSubject == googleSubject);

        public Task<bool> TenDangNhapDaTonTaiAsync(string tenDangNhap, int? boQuaMa = null) =>
            _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap && (boQuaMa == null || t.MaTaiKhoan != boQuaMa));

        public Task<bool> EmailDaTonTaiAsync(string? email, int? boQuaMa = null) =>
            _context.TaiKhoans.AnyAsync(t => t.Email == email && (boQuaMa == null || t.MaTaiKhoan != boQuaMa));

        public Task<bool> TonTaiAsync(int id) =>
            _context.TaiKhoans.AnyAsync(t => t.MaTaiKhoan == id);

        public Task<TaiKhoan?> LayTheoMaCoKhachHangAsync(int id) =>
            _context.TaiKhoans.Include(t => t.KhachHang).AsNoTracking().FirstOrDefaultAsync(m => m.MaTaiKhoan == id);

        public Task<TaiKhoan?> LayTheoMaCoKhachHangTheoDoiAsync(int id) =>
            _context.TaiKhoans.Include(t => t.KhachHang).FirstOrDefaultAsync(m => m.MaTaiKhoan == id);

        public async Task<List<TaiKhoan>> LayTatCaChuaLienKetKhachHangAsync()
        {
            return await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && !_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();
        }

        public async Task<List<TaiKhoan>> LayTatCaCoTheLienKetAsync(int? currentMaTaiKhoan = null)
        {
            return await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && (!_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan) || (currentMaTaiKhoan != null && t.MaTaiKhoan == currentMaTaiKhoan)))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();
        }

        public async Task ThemAsync(TaiKhoan taiKhoan)
        {
            _context.Add(taiKhoan);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(TaiKhoan taiKhoan)
        {
            _context.Update(taiKhoan);
            await _context.SaveChangesAsync();
        }

        public async Task XoaAsync(TaiKhoan taiKhoan)
        {
            _context.Remove(taiKhoan);
            await _context.SaveChangesAsync();
        }
    }
}