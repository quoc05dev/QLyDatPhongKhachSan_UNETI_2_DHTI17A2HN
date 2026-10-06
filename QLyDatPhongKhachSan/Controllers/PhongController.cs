// Ho va ten: Dau Duc Luu
// Ma sinh vien: 23103100114
// Noi dung: CRUD co ban quan ly Phong
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLyDatPhongKhachSan.Controllers
{
    public class PhongController : Controller
    {
        private readonly AppDbContext _context;

        public PhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Phong
        // Tạm thời để khách vãng lai xem danh sách phòng (xem công khai).
        // Phần ghi vẫn chỉ Admin và Nhân viên, xem Index/Create/Edit/Delete.
        public async Task<IActionResult> Index(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax)
        {
            bool isPriceFilterValid = true;

            // Lỗi binding (nhập chữ thay vì số) của framework là tiếng Anh,
            // đổi sang tiếng Việt cho khớp giao diện.
            foreach (var ten in new[] { "GiaMin", "GiaMax" })
            {
                if (ModelState.TryGetValue(ten, out var entry) && entry.Errors.Count > 0)
                {
                    entry.Errors.Clear();
                    ModelState.AddModelError(ten, ten == "GiaMin"
                        ? "Giá tối thiểu không hợp lệ. Vui lòng nhập một số."
                        : "Giá tối đa không hợp lệ. Vui lòng nhập một số.");
                    isPriceFilterValid = false;
                }
            }

            if (giaMin.HasValue && giaMin.Value < 0)
            {
                ModelState.AddModelError("GiaMin", "Giá tối thiểu phải lớn hơn hoặc bằng 0.");
                isPriceFilterValid = false;
            }
            if (giaMax.HasValue && giaMax.Value < 0)
            {
                ModelState.AddModelError("GiaMax", "Giá tối đa phải lớn hơn hoặc bằng 0.");
                isPriceFilterValid = false;
            }
            if (giaMin.HasValue && giaMax.HasValue && giaMin.Value > giaMax.Value)
            {
                ModelState.AddModelError("GiaMin", "Giá tối thiểu không được lớn hơn giá tối đa.");
                isPriceFilterValid = false;
            }

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

            if (isPriceFilterValid)
            {
                if (giaMin.HasValue)
                {
                    query = query.Where(p => p.DonGia >= giaMin.Value);
                }
                if (giaMax.HasValue)
                {
                    query = query.Where(p => p.DonGia <= giaMax.Value);
                }
            }

            var phongs = await query.ToListAsync();

            var loaiPhongs = await _context.LoaiPhongs.AsNoTracking().OrderBy(l => l.TenLoai).ToListAsync();
            var trangThaiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Trong", Text = "Trong" },
                new SelectListItem { Value = "DangXuLy", Text = "DangXuLy" },
                new SelectListItem { Value = "DaDat", Text = "DaDat" },
                new SelectListItem { Value = "DangSuDung", Text = "DangSuDung" },
                new SelectListItem { Value = "BaoTri", Text = "BaoTri" }
            };

            var viewModel = new PhongListViewModel
            {
                Phongs = phongs,
                Keyword = keyword,
                MaLoaiPhong = maLoaiPhong,
                Tang = tang,
                TrangThai = trangThai,
                GiaMin = giaMin,
                GiaMax = giaMax,
                LoaiPhongList = new SelectList(loaiPhongs, "MaLoaiPhong", "TenLoai", maLoaiPhong),
                TrangThaiList = new SelectList(trangThaiList, "Value", "Text", trangThai)
            };
            return View(viewModel);
        }

        // GET: Phong/Details/5
        // Tạm thời cho phép xem công khai, giống Index. Xem Create/Edit/Delete.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaPhong == id);
                
            if (phong == null)
            {
                return NotFound();
            }

            return View(phong);
        }

        private void PopulateDropdowns(int? selectedLoaiPhong = null, string? selectedTrangThai = null)
        {
            ViewBag.MaLoaiPhong = new SelectList(_context.LoaiPhongs.AsNoTracking(), "MaLoaiPhong", "TenLoai", selectedLoaiPhong);
            
            var trangThaiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Trong", Text = "Trong" },
                new SelectListItem { Value = "DangXuLy", Text = "DangXuLy" },
                new SelectListItem { Value = "DaDat", Text = "DaDat" },
                new SelectListItem { Value = "DangSuDung", Text = "DangSuDung" },
                new SelectListItem { Value = "BaoTri", Text = "BaoTri" }
            };
            ViewBag.TrangThai = new SelectList(trangThaiList, "Value", "Text", selectedTrangThai);
        }

        // GET: Phong/Create
        [RoleAuthorize("Admin", "NhanVien")]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Phong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Create([Bind("SoPhong,MaLoaiPhong,Tang,HuongPhong,TienNghi,DonGia,TrangThai,GhiChu,NgayBaoTri,GhiChuBaoTri")] Phong phong)
        {
            if (ModelState.IsValid)
            {
                bool isDuplicate = await _context.Phongs.AnyAsync(p => p.SoPhong == phong.SoPhong);
                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Phong.SoPhong), "Số phòng đã tồn tại.");
                }
                else
                {
                    _context.Add(phong);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            PopulateDropdowns(phong.MaLoaiPhong, phong.TrangThai);
            return View(phong);
        }

        // GET: Phong/Edit/5
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phong = await _context.Phongs.FindAsync(id);
            if (phong == null)
            {
                return NotFound();
            }
            PopulateDropdowns(phong.MaLoaiPhong, phong.TrangThai);
            return View(phong);
        }

        // POST: Phong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhong,SoPhong,MaLoaiPhong,Tang,HuongPhong,TienNghi,DonGia,TrangThai,GhiChu,NgayBaoTri,GhiChuBaoTri")] Phong phong)
        {
            if (id != phong.MaPhong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                bool isDuplicate = await _context.Phongs.AnyAsync(p => p.SoPhong == phong.SoPhong && p.MaPhong != phong.MaPhong);
                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Phong.SoPhong), "Số phòng đã tồn tại.");
                }
                else
                {
                    try
                    {
                        _context.Update(phong);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!PhongExists(phong.MaPhong))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            PopulateDropdowns(phong.MaLoaiPhong, phong.TrangThai);
            return View(phong);
        }

        // GET: Phong/Delete/5
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaPhong == id);
                
            if (phong == null)
            {
                return NotFound();
            }

            bool hasHistory = await _context.ChiTietDatPhongs.AnyAsync(ct => ct.MaPhong == id);
            ViewBag.HasHistory = hasHistory;

            return View(phong);
        }

        // POST: Phong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasHistory = await _context.ChiTietDatPhongs.AnyAsync(ct => ct.MaPhong == id);
            if (hasHistory)
            {
                TempData["ErrorMessage"] = "Không thể xóa phòng vì phòng đã phát sinh lịch sử đặt phòng.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            var phong = await _context.Phongs.FindAsync(id);
            if (phong != null)
            {
                try
                {
                    _context.Phongs.Remove(phong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Không thể xóa phòng vì dữ liệu liên quan đã phát sinh hoặc vừa được cập nhật.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool PhongExists(int id)
        {
            return _context.Phongs.Any(e => e.MaPhong == id);
        }
    }
}
