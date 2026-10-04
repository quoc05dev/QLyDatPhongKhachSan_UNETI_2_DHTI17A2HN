// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Quản lý Dữ liệu nền (Loại phòng) dành cho Admin (CRUD, LINQ, Kiểm tra trùng tên & Tham chiếu).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin")]
    public class LoaiPhongController : Controller
    {
        private readonly AppDbContext _context;

        public LoaiPhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: LoaiPhong
        public async Task<IActionResult> Index(string searchTerm, bool? statusFilter, int page = 1)
        {
            int pageSize = 5;
            var query = _context.LoaiPhongs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.TenLoai.ToLower().Contains(term) ||
                                         (l.MoTa != null && l.MoTa.ToLower().Contains(term)));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(l => l.TrangThai == statusFilter.Value);
            }

            query = query.OrderByDescending(l => l.MaLoaiPhong);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.SearchTerm = searchTerm ?? string.Empty;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        // GET: LoaiPhong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loaiPhong = await _context.LoaiPhongs
                .Include(l => l.Phongs)
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);

            if (loaiPhong == null) return NotFound();

            return View(loaiPhong);
        }

        // GET: LoaiPhong/Create
        public IActionResult Create()
        {
            return View(new LoaiPhong());
        }

        // POST: LoaiPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenLoai,SoNguoiToiDa,DonGiaNgay,MoTa,HinhAnh,TrangThai")] LoaiPhong loaiPhong)
        {
            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            // Kiểm tra trùng Tên loại phòng bằng LINQ
            if (await _context.LoaiPhongs.AnyAsync(l => l.TenLoai.ToLower() == loaiPhong.TenLoai.Trim().ToLower()))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã tồn tại. Vui lòng nhập tên khác.");
                return View(loaiPhong);
            }

            _context.Add(loaiPhong);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm mới loại phòng '{loaiPhong.TenLoai}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: LoaiPhong/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null) return NotFound();

            return View(loaiPhong);
        }

        // POST: LoaiPhong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLoaiPhong,TenLoai,SoNguoiToiDa,DonGiaNgay,MoTa,HinhAnh,TrangThai")] LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            // Kiểm tra trùng Tên loại phòng ngoại trừ bản ghi hiện tại
            if (await _context.LoaiPhongs.AnyAsync(l => l.TenLoai.ToLower() == loaiPhong.TenLoai.Trim().ToLower() && l.MaLoaiPhong != id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã bị trùng với loại phòng khác.");
                return View(loaiPhong);
            }

            try
            {
                _context.Update(loaiPhong);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật loại phòng '{loaiPhong.TenLoai}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.LoaiPhongs.AnyAsync(e => e.MaLoaiPhong == id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: LoaiPhong/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null) return NotFound();

            loaiPhong.TrangThai = !loaiPhong.TrangThai;
            _context.Update(loaiPhong);
            await _context.SaveChangesAsync();

            string statusText = loaiPhong.TrangThai ? "mở khóa" : "khóa";
            TempData["SuccessMessage"] = $"Đã {statusText} loại phòng '{loaiPhong.TenLoai}' thành công!";

            return RedirectToAction(nameof(Index));
        }

        // GET: LoaiPhong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var loaiPhong = await _context.LoaiPhongs
                .Include(l => l.Phongs)
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == id);

            if (loaiPhong == null) return NotFound();

            return View(loaiPhong);
        }

        // POST: LoaiPhong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null) return NotFound();

            // Kiểm tra ràng buộc tham chiếu: Không xóa nếu đã có phòng thuộc loại này
            bool hasPhongs = await _context.Phongs.AnyAsync(p => p.MaLoaiPhong == id);
            if (hasPhongs)
            {
                TempData["ErrorMessage"] = $"Không thể xóa loại phòng '{loaiPhong.TenLoai}' vì đã có các phòng trong CSDL tham chiếu tới loại phòng này!";
                return RedirectToAction(nameof(Index));
            }

            _context.LoaiPhongs.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa loại phòng '{loaiPhong.TenLoai}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
