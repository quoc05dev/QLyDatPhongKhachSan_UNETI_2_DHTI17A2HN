using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Repositories
{
    public class KhachHangRepository : GenericRepository<KhachHang>, IKhachHangRepository
    {
        private readonly AppDbContext _context;

        public KhachHangRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<KhachHang?> LayTheoMaTaiKhoanAsync(int maTaiKhoan) =>
            _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);

        public Task<KhachHang?> LayTheoMaTaiKhoanCoDatPhongAsync(int maTaiKhoan) =>
            _context.KhachHangs.Include(k => k.DatPhongs).FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);

        public Task<KhachHang?> LayTheoMaTaiKhoanCoTaiKhoanAsync(int maTaiKhoan) =>
            _context.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);

        public Task<KhachHang?> LayTheoMaAsync(int maKhachHang) =>
            _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

        public Task<KhachHang?> LayTheoMaCoTaiKhoanAsync(int maKhachHang) =>
            _context.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

        public Task<KhachHang?> LayTheoMaCoTaiKhoanVaDatPhongFullAsync(int maKhachHang) =>
            _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                    .ThenInclude(dp => dp.ChiTietDatPhongs)
                        .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(m => m.MaKhachHang == maKhachHang);

        public async Task<int> DemTatCaAsync() => await _context.KhachHangs.CountAsync();

        public async Task<int> DemKhachHangAsync() => await _context.KhachHangs.CountAsync();

        public async Task<int> DemTheoTrangThaiAsync(bool trangThai) => await _context.KhachHangs.CountAsync(k => k.TrangThai == trangThai);

        public async Task<int> DemCoTaiKhoanAsync(bool coTaiKhoan)
        {
            if (coTaiKhoan)
                return await _context.KhachHangs.CountAsync(k => k.MaTaiKhoan != null);
            return await _context.KhachHangs.CountAsync(k => k.MaTaiKhoan == null);
        }

        public async Task<bool> CCCDDaTonTaiAsync(string cccd, int? boQuaMa = null)
        {
            if (string.IsNullOrWhiteSpace(cccd)) return false;
            cccd = cccd.Trim();
            return await _context.KhachHangs.AnyAsync(kh => kh.CCCD == cccd && (boQuaMa == null || kh.MaKhachHang != boQuaMa));
        }

        public async Task<bool> EmailDaTonTaiAsync(string? email, int? boQuaMa = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            email = email.Trim();
            return await _context.KhachHangs.AnyAsync(kh => kh.Email == email && (boQuaMa == null || kh.MaKhachHang != boQuaMa));
        }

        public async Task<List<KhachHang>> TimKiemVaLocPhanTrangAsync(string? searchTerm, string? gioiTinhFilter, bool? statusFilter, string? coTaiKhoanFilter, string sortOrder, int skip, int take)
        {
            var query = _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(k => k.HoTen.ToLower().Contains(term) ||
                                         k.SoDienThoai.Contains(term) ||
                                         k.CCCD.Contains(term) ||
                                         (k.Email != null && k.Email.ToLower().Contains(term)) ||
                                         (k.DiaChi != null && k.DiaChi.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(gioiTinhFilter))
            {
                query = query.Where(k => k.GioiTinh == gioiTinhFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(k => k.TrangThai == statusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(coTaiKhoanFilter))
            {
                if (coTaiKhoanFilter == "co")
                    query = query.Where(k => k.MaTaiKhoan != null);
                else if (coTaiKhoanFilter == "khong")
                    query = query.Where(k => k.MaTaiKhoan == null);
            }

            sortOrder = string.IsNullOrEmpty(sortOrder) ? "id_desc" : sortOrder;
            query = sortOrder switch
            {
                "id_asc" => query.OrderBy(k => k.MaKhachHang),
                "name_asc" => query.OrderBy(k => k.HoTen),
                "name_desc" => query.OrderByDescending(k => k.HoTen),
                _ => query.OrderByDescending(k => k.MaKhachHang)
            };

            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<int> DemTimKiemVaLocAsync(string? searchTerm, string? gioiTinhFilter, bool? statusFilter, string? coTaiKhoanFilter)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(k => k.HoTen.ToLower().Contains(term) ||
                                         k.SoDienThoai.Contains(term) ||
                                         k.CCCD.Contains(term) ||
                                         (k.Email != null && k.Email.ToLower().Contains(term)) ||
                                         (k.DiaChi != null && k.DiaChi.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(gioiTinhFilter))
            {
                query = query.Where(k => k.GioiTinh == gioiTinhFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(k => k.TrangThai == statusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(coTaiKhoanFilter))
            {
                if (coTaiKhoanFilter == "co")
                    query = query.Where(k => k.MaTaiKhoan != null);
                else if (coTaiKhoanFilter == "khong")
                    query = query.Where(k => k.MaTaiKhoan == null);
            }

            return await query.CountAsync();
        }

        public async Task ThemAsync(KhachHang khachHang)
        {
            _context.Add(khachHang);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(KhachHang khachHang)
        {
            _context.Update(khachHang);
            await _context.SaveChangesAsync();
        }

        public async Task XoaAsync(KhachHang khachHang)
        {
            _context.Remove(khachHang);
            await _context.SaveChangesAsync();
        }
    }
}