using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Repositories
{
    public class DichVuRepository : GenericRepository<DichVu>, IDichVuRepository
    {
        private readonly AppDbContext _context;

        public DichVuRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<DichVu>> TimKiem(string? searchTerm)
        {
            var query = _context.DichVus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(dv => dv.TenDichVu.ToLower().Contains(term));
            }

            return await query.OrderByDescending(dv => dv.MaDichVu).ToListAsync();
        }

        public Task<DichVu?> LayTheoMaAsync(int id) =>
            _context.DichVus.AsNoTracking().FirstOrDefaultAsync(dv => dv.MaDichVu == id);

        public Task<bool> TenDaTonTaiAsync(string tenDichVu, int? boQuaMa) =>
            _context.DichVus.AnyAsync(dv => dv.TenDichVu == tenDichVu && (boQuaMa == null || dv.MaDichVu != boQuaMa));

        public Task<bool> TonTaiAsync(int id) =>
            _context.DichVus.AnyAsync(dv => dv.MaDichVu == id);

        public async Task ThemAsync(DichVu dichVu)
        {
            _context.Add(dichVu);
            await _context.SaveChangesAsync();
        }

        public async Task CapNhatAsync(DichVu dichVu)
        {
            _context.Update(dichVu);
            await _context.SaveChangesAsync();
        }

        public async Task XoaAsync(DichVu dichVu)
        {
            _context.Remove(dichVu);
            await _context.SaveChangesAsync();
        }
    }
}
