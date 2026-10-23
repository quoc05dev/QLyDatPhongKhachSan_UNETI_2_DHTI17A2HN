using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Repositories
{
    public class PhongRepository : GenericRepository<Phong>, IPhongRepository
    {
        private readonly AppDbContext _context;

        public PhongRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Phong>> TimKiemVaLocAsync(string? searchTerm, int? maLoaiPhong, string? trangThai, string sortOrder)
        {
            var query = _context.Phongs.Include(p => p.LoaiPhong).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(p => p.SoPhong.ToLower().Contains(term));
            }

            if (maLoaiPhong.HasValue)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            sortOrder ??= "room_asc";
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.DonGia),
                "price_desc" => query.OrderByDescending(p => p.DonGia),
                "floor_asc" => query.OrderBy(p => p.Tang),
                "floor_desc" => query.OrderByDescending(p => p.Tang),
                _ => query.OrderBy(p => p.SoPhong)
            };

            return await query.ToListAsync();
        }

        public async Task<List<Phong>> LayPhongHienThiTrangChuAsync() =>
            await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.LoaiPhong != null && p.LoaiPhong.TrangThai && p.TrangThai != "BaoTri")
                .ToListAsync();

        public async Task<List<Phong>> LayDanhSachPhongKhongBaoTriAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != "BaoTri")
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public Task<Phong?> LayTheoMaAsync(int id) =>
            _context.Phongs.AsNoTracking().Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == id);

        public Task<Phong?> LayTheoMaCoLoaiPhongAsync(int id) =>
            _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == id);

        public Task<Phong?> LayTheoMaTheoDoiAsync(int id) =>
            _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == id);

        public Task<bool> SoPhongDaTonTaiAsync(string soPhong, int? boQuaMa = null) =>
            _context.Phongs.AnyAsync(p => p.SoPhong == soPhong && (boQuaMa == null || p.MaPhong != boQuaMa));

        public Task<bool> TonTaiPhongThuocLoaiAsync(int maLoaiPhong) =>
            _context.Phongs.AnyAsync(p => p.MaLoaiPhong == maLoaiPhong);

        public Task<IQueryable<Phong>> QueryCoLoaiPhongNoTracking()
        {
            return Task.FromResult(_context.Phongs.Include(p => p.LoaiPhong).AsNoTracking().AsQueryable());
        }

        public async Task<List<Phong>> TimKiemVaLocFullAsync(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, DateTime? ngayNhan, DateTime? ngayTra, string? sortOrder, int skip, int take)
        {
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p =>
                    p.SoPhong.Contains(keyword) ||
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.Contains(keyword)) ||
                    p.ChiTietDatPhongs.Any(ct =>
                        ct.DatPhong != null &&
                        ct.DatPhong.KhachHang != null &&
                        ct.DatPhong.KhachHang.HoTen.Contains(keyword)));
            }

            if (maLoaiPhong.HasValue)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong);
            }

            if (tang.HasValue)
            {
                query = query.Where(p => p.Tang == tang);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            if (giaMin.HasValue)
            {
                query = query.Where(p => p.DonGia >= giaMin.Value);
            }
            if (giaMax.HasValue)
            {
                query = query.Where(p => p.DonGia <= giaMax.Value);
            }

            if (ngayNhan.HasValue && ngayTra.HasValue)
            {
                var requestedStart = ngayNhan.Value.Date;
                var requestedEnd = ngayTra.Value.Date;
                query = query.Where(p =>
                    !p.ChiTietDatPhongs.Any(ct =>
                        ct.DatPhong != null
                        && ct.DatPhong.TrangThai != "DaHuy"
                        && ct.NgayNhan.Date < requestedEnd
                        && ct.NgayTra.Date > requestedStart));
            }

            switch (sortOrder)
            {
                case "soPhong_desc":
                    query = query.OrderByDescending(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "donGia_asc":
                    query = query.OrderBy(p => p.DonGia).ThenBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "donGia_desc":
                    query = query.OrderByDescending(p => p.DonGia).ThenBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "tang_asc":
                    query = query.OrderBy(p => p.Tang).ThenBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "tang_desc":
                    query = query.OrderByDescending(p => p.Tang).ThenBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "loaiPhong_asc":
                    query = query.OrderBy(p => p.LoaiPhong != null ? p.LoaiPhong.TenLoai : "").ThenBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
                case "soPhong_asc":
                default:
                    query = query.OrderBy(p => p.SoPhong).ThenBy(p => p.MaPhong);
                    break;
            }

            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<int> DemTimKiemVaLocFullAsync(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, DateTime? ngayNhan, DateTime? ngayTra)
        {
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p =>
                    p.SoPhong.Contains(keyword) ||
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.Contains(keyword)) ||
                    p.ChiTietDatPhongs.Any(ct =>
                        ct.DatPhong != null &&
                        ct.DatPhong.KhachHang != null &&
                        ct.DatPhong.KhachHang.HoTen.Contains(keyword)));
            }

            if (maLoaiPhong.HasValue)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong);
            }

            if (tang.HasValue)
            {
                query = query.Where(p => p.Tang == tang);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            if (giaMin.HasValue)
            {
                query = query.Where(p => p.DonGia >= giaMin.Value);
            }
            if (giaMax.HasValue)
            {
                query = query.Where(p => p.DonGia <= giaMax.Value);
            }

            if (ngayNhan.HasValue && ngayTra.HasValue)
            {
                var requestedStart = ngayNhan.Value.Date;
                var requestedEnd = ngayTra.Value.Date;
                query = query.Where(p =>
                    !p.ChiTietDatPhongs.Any(ct =>
                        ct.DatPhong != null
                        && ct.DatPhong.TrangThai != "DaHuy"
                        && ct.NgayNhan.Date < requestedEnd
                        && ct.NgayTra.Date > requestedStart));
            }

            return await query.CountAsync();
        }

        public Task<bool> CoLichSuDatPhongAsync(int maPhong) =>
            _context.ChiTietDatPhongs.AnyAsync(ct => ct.MaPhong == maPhong);

        public async Task<List<Phong>> LayDanhSachPhongTrongAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == "Trong")
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<List<Phong>> LayTatCaAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<List<Phong>> TimKiemVaLocPhongDatPhongAsync(string? keyword, int? maLoaiPhong)
        {
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != "BaoTri")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.SoPhong.Contains(keyword) ||
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.Contains(keyword)));
            }

            if (maLoaiPhong.HasValue)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }

            return await query.OrderBy(p => p.SoPhong).ToListAsync();
        }

        public async Task<List<Phong>> TimKiemVaLocPhongDatPhongAsync(int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, string? keyword)
        {
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != "BaoTri")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.SoPhong.Contains(keyword) ||
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.Contains(keyword)));
            }

            if (maLoaiPhong.HasValue)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }

            if (tang.HasValue)
            {
                query = query.Where(p => p.Tang == tang);
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            if (giaMin.HasValue)
            {
                query = query.Where(p => p.DonGia >= giaMin.Value);
            }
            if (giaMax.HasValue)
            {
                query = query.Where(p => p.DonGia <= giaMax.Value);
            }

            return await query.OrderBy(p => p.SoPhong).ToListAsync();
        }

        public async Task<int> DemPhongKhongBaoTriTheoLoaiAsync(int maLoaiPhong)
        {
            return await _context.Phongs.CountAsync(p => p.MaLoaiPhong == maLoaiPhong && p.TrangThai != "BaoTri");
        }

        public async Task<List<Phong>> LayPhongTrongCungLoaiAsync(int maLoaiPhong)
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.MaLoaiPhong == maLoaiPhong && p.TrangThai == "Trong")
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<List<Phong>> LayPhongTrongCungLoaiAsync(int maLoaiPhong, List<int>? phongDaChonTrongDon, List<int>? phongBanIds)
        {
            phongDaChonTrongDon ??= new List<int>();
            phongBanIds ??= new List<int>();
            var exclude = phongDaChonTrongDon.Concat(phongBanIds).Distinct().ToList();
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.MaLoaiPhong == maLoaiPhong && p.TrangThai == "Trong")
                .AsQueryable();

            if (exclude.Count > 0)
            {
                query = query.Where(p => !exclude.Contains(p.MaPhong));
            }

            return await query.OrderBy(p => p.SoPhong).ToListAsync();
        }

        public async Task ThemAsync(Phong phong)
        {
            _context.Add(phong);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(Phong phong)
        {
            _context.Update(phong);
            await _context.SaveChangesAsync();
        }

        public async Task XoaAsync(Phong phong)
        {
            _context.Remove(phong);
            await _context.SaveChangesAsync();
        }
    }
}