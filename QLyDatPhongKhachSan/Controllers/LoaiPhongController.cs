// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Controller Quản lý Loại phòng (CRUD, tìm kiếm,
// lọc theo trạng thái, sắp xếp, phân trang LINQ và kiểm tra tham chiếu khi xóa).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin")]
    public class LoaiPhongController : Controller
    {
        private readonly ILoaiPhongRepository _loaiPhongRepository;
        private readonly IPhongRepository _phongRepository;

        public LoaiPhongController(ILoaiPhongRepository loaiPhongRepository, IPhongRepository phongRepository)
        {
            _loaiPhongRepository = loaiPhongRepository;
            _phongRepository = phongRepository;
        }

        // GET: LoaiPhong
        public async Task<IActionResult> Index(string searchTerm, bool? statusFilter, string sortOrder, int page = 1)
        {
            int pageSize = 5; // Số bản ghi mỗi trang theo quy định phân trang
            var allItems = await _loaiPhongRepository.TimKiemVaLocAsync(searchTerm, statusFilter, sortOrder);

            int totalItems = allItems.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new LoaiPhongListViewModel
            {
                Items = items,
                SearchTerm = searchTerm ?? string.Empty,
                StatusFilter = statusFilter,
                SortOrder = sortOrder ?? "name_asc",
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

            var loaiPhong = await _loaiPhongRepository.LayTheoMaVoiPhongAsync(id.Value);

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
            if (await _loaiPhongRepository.TenDaTonTaiAsync(loaiPhong.TenLoai))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            await _loaiPhongRepository.ThemAsync(loaiPhong);

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

            var loaiPhong = await _loaiPhongRepository.FindAsync(id.Value);
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
            if (await _loaiPhongRepository.TenDaTonTaiAsync(loaiPhong.TenLoai, id))
            {
                ModelState.AddModelError("TenLoai", "Tên loại phòng đã bị trùng với loại phòng khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(loaiPhong);
            }

            try
            {
                await _loaiPhongRepository.CapNhatAsync(loaiPhong);
                TempData["SuccessMessage"] = $"Cập nhật loại phòng '{loaiPhong.TenLoai}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _loaiPhongRepository.FindAsync(id);
                if (exists == null)
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

            var loaiPhong = await _loaiPhongRepository.LayTheoMaVoiPhongAsync(id.Value);

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
            var loaiPhong = await _loaiPhongRepository.FindAsync(id);
            if (loaiPhong == null)
            {
                return NotFound();
            }

            // Kiểm tra ràng buộc tham chiếu: không xóa loại phòng đang có phòng sử dụng
            bool dangDuocSuDung = await _phongRepository.TonTaiPhongThuocLoaiAsync(id);
            if (dangDuocSuDung)
            {
                TempData["ErrorMessage"] = $"Không thể xóa loại phòng '{loaiPhong.TenLoai}' vì đang có phòng thuộc loại này!";
                return RedirectToAction(nameof(Index));
            }

            await _loaiPhongRepository.XoaAsync(loaiPhong);

            TempData["SuccessMessage"] = $"Đã xóa loại phòng '{loaiPhong.TenLoai}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
