// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Module 5 - Quản lý dịch vụ khách sạn (CRUD, tìm kiếm, validation).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: QUẢN LÝ DỊCH VỤ
    // ==========================================
    [RoleAuthorize("Admin", "NhanVien")]
    public class DichVuController : Controller
    {
        private readonly IDichVuRepository _dichVuRepository;

        public DichVuController(IDichVuRepository dichVuRepository)
        {
            _dichVuRepository = dichVuRepository;
        }

        // GET: DichVu
        public async Task<IActionResult> Index(string searchTerm)
        {
            var danhSach = await _dichVuRepository.TimKiem(searchTerm);

            ViewData["SearchTerm"] = searchTerm ?? string.Empty;

            return View(danhSach);
        }

        // GET: DichVu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _dichVuRepository.LayTheoMaAsync(id.Value);
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
            if (await _dichVuRepository.TenDaTonTaiAsync(dichVu.TenDichVu))
            {
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(dichVu);
            }

            await _dichVuRepository.ThemAsync(dichVu);

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

            var dichVu = await _dichVuRepository.FindAsync(id.Value);
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

            if (await _dichVuRepository.TenDaTonTaiAsync(dichVu.TenDichVu, id))
            {
                ModelState.AddModelError("TenDichVu", "Tên dịch vụ đã bị trùng với dịch vụ khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(dichVu);
            }

            try
            {
                await _dichVuRepository.CapNhatAsync(dichVu);
                TempData["SuccessMessage"] = $"Cập nhật dịch vụ '{dichVu.TenDichVu}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _dichVuRepository.TonTaiAsync(id))
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

            var dichVu = await _dichVuRepository.LayTheoMaAsync(id.Value);
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
            var dichVu = await _dichVuRepository.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }

            await _dichVuRepository.XoaAsync(dichVu);

            TempData["SuccessMessage"] = $"Đã xóa dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}

