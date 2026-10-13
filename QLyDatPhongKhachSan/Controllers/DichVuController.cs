// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Module 5 - Quản lý dịch vụ khách sạn (CRUD, tìm kiếm, validation).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: QUẢN LÝ DỊCH VỤ
    // ==========================================
    [RoleAuthorize("Admin", "NhanVien")]
    public class DichVuController : Controller
    {
        private readonly AppDbContext _context;

        public DichVuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DichVu
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.DichVus.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(dv => dv.TenDichVu.ToLower().Contains(term));
            }

            ViewData["SearchTerm"] = searchTerm ?? string.Empty;

            var danhSach = await query
                .OrderByDescending(dv => dv.MaDichVu)
                .ToListAsync();

            return View(danhSach);
        }

        // GET: DichVu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus.FirstOrDefaultAsync(dv => dv.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // GET: DichVu/Create
        public IActionResult Create()
        {
            return View(new DichVu());
        }

        // POST: DichVu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDichVu,Gia,MoTa,HinhAnh,TrangThai")] DichVu dichVu)
        {
            if (await _context.DichVus.AnyAsync(dv => dv.TenDichVu == dichVu.TenDichVu))
            {
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(dichVu);
            }

            _context.Add(dichVu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm mới dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: DichVu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // POST: DichVu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDichVu,TenDichVu,Gia,MoTa,HinhAnh,TrangThai")] DichVu dichVu)
        {
            if (id != dichVu.MaDichVu)
            {
                return NotFound();
            }

            if (await _context.DichVus.AnyAsync(dv => dv.TenDichVu == dichVu.TenDichVu && dv.MaDichVu != id))
            {
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã bị trùng với dịch vụ khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(dichVu);
            }

            try
            {
                _context.Update(dichVu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật dịch vụ '{dichVu.TenDichVu}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.DichVus.AnyAsync(dv => dv.MaDichVu == id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: DichVu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus.FirstOrDefaultAsync(dv => dv.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // POST: DichVu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }

            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
