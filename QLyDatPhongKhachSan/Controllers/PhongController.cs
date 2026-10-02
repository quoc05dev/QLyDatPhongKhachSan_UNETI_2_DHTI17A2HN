// Ho va ten: Dau Duc Luu
// Ma sinh vien: 23103100114
// Noi dung: CRUD co ban quan ly Phong
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
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
        public async Task<IActionResult> Index(string? keyword)
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
                    (p.LoaiPhong != null && p.LoaiPhong.TenLoai.Contains(keyword)));
            }

            var phongs = await query.ToListAsync();
            var viewModel = new PhongListViewModel
            {
                Phongs = phongs,
                Keyword = keyword
            };
            return View(viewModel);
        }

        // GET: Phong/Details/5
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
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Phong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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
