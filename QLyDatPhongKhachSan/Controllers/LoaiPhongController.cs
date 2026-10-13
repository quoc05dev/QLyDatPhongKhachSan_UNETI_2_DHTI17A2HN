// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Controller Quản lý Loại phòng (CRUD, tìm kiếm,
// lọc theo trạng thái, sắp xếp, phân trang LINQ và kiểm tra tham chiếu khi xóa).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

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
        public async Task<IActionResult> Index(string searchTerm, bool? statusFilter, string sortOrder, int page = 1)
        {
            int pageSize = 5; // Số bản ghi mỗi trang theo quy định phân trang
            var query = _context.LoaiPhongs.Include(l => l.Phongs).AsQueryable();

            // 1. Tìm kiếm theo tên loại phòng
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.TenLoai.ToLower().Contains(term));
            }

            // 2. Lọc theo trạng thái hoạt động
            if (statusFilter.HasValue)
            {
                query = query.Where(l => l.TrangThai == statusFilter.Value);
            }

            // 3. Sắp xếp theo tên, đơn giá, sức chứa
            sortOrder ??= "name_asc";
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(l => l.DonGiaNgay),
                "price_desc" => query.OrderByDescending(l => l.DonGiaNgay),
                "capacity_desc" => query.OrderByDescending(l => l.SoNguoiToiDa),
                _ => query.OrderBy(l => l.TenLoai)
            };

            // 4. Phân trang bằng Skip() / Take() trên LINQ EF Core
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new LoaiPhongListViewModel
            {
                Items = items,
                SearchTerm = searchTerm ?? string.Empty,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,
                PageIndex = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: LoaiPhong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs
                .Include(l => l.Phongs)
                .FirstOrDefaultAsync(l => l.MaLoaiPhong == id);

            if (loaiPhong == null)
            {
                return NotFound();
            }

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
            // Kiểm tra trùng tên loại phòng bằng LINQ
            if (await _context.LoaiPhongs.AnyAsync(l => l.TenLoai == loaiPhong.TenLoai))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
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
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // POST: LoaiPhong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLoaiPhong,TenLoai,SoNguoiToiDa,DonGiaNgay,MoTa,HinhAnh,TrangThai")] LoaiPhong loaiPhong)
        {
            if (id != loaiPhong.MaLoaiPhong)
            {
                return NotFound();
            }

            // Kiểm tra trùng tên loại phòng ngoại trừ bản ghi hiện tại
            if (await _context.LoaiPhongs.AnyAsync(l => l.TenLoai == loaiPhong.TenLoai && l.MaLoaiPhong != id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã bị trùng với loại phòng khác.");
            }

            if (!ModelState.IsValid)
            {
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
                if (!await _context.LoaiPhongs.AnyAsync(l => l.MaLoaiPhong == id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: LoaiPhong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiPhong = await _context.LoaiPhongs
                .Include(l => l.Phongs)
                .FirstOrDefaultAsync(l => l.MaLoaiPhong == id);

            if (loaiPhong == null)
            {
                return NotFound();
            }

            return View(loaiPhong);
        }

        // POST: LoaiPhong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loaiPhong = await _context.LoaiPhongs.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            // Kiểm tra ràng buộc tham chiếu: không xóa loại phòng đang có phòng sử dụng
            bool dangDuocSuDung = await _context.Phongs.AnyAsync(p => p.MaLoaiPhong == id);
            if (dangDuocSuDung)
            {
                TempData["ErrorMessage"] = $"Không thể xóa loại phòng '{loaiPhong.TenLoai}' vì đang có phòng thuộc loại này!";
                return RedirectToAction(nameof(Index));
            }

            _context.LoaiPhongs.Remove(loaiPhong);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa loại phòng '{loaiPhong.TenLoai}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
