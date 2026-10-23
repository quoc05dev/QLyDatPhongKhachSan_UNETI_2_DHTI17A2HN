// Ho va ten: Dau Duc Luu
// Ma sinh vien: 23103100114
// Noi dung: CRUD co ban quan ly Phong
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLyDatPhongKhachSan.Controllers
{
    public class PhongController : Controller
    {
        private readonly IPhongRepository _phongRepository;
        private readonly ILoaiPhongRepository _loaiPhongRepository;
        private readonly IChiTietDatPhongRepository _chiTietDatPhongRepository;

        public PhongController(IPhongRepository phongRepository, ILoaiPhongRepository loaiPhongRepository, IChiTietDatPhongRepository chiTietDatPhongRepository)
        {
            _phongRepository = phongRepository;
            _loaiPhongRepository = loaiPhongRepository;
            _chiTietDatPhongRepository = chiTietDatPhongRepository;
        }

        // GET: Phong
        // Màn hình quản lý phòng chỉ dành cho Admin và Nhân viên (khách hàng không được truy cập).
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Index(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, DateTime? ngayNhan, DateTime? ngayTra, string? sortOrder, int page = 1)
        {
            if (ModelState.TryGetValue("page", out var pageEntry) && pageEntry.Errors.Count > 0)
            {
                pageEntry.Errors.Clear();
                page = 1;
            }

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

            bool isAvailabilityFilterValid = true;

            bool ngayNhanHasBindingError = false;
            if (ModelState.TryGetValue("NgayNhan", out var nnEntry) && nnEntry.Errors.Count > 0)
            {
                nnEntry.Errors.Clear();
                ModelState.AddModelError("NgayNhan", "Ngày nhận không hợp lệ.");
                isAvailabilityFilterValid = false;
                ngayNhanHasBindingError = true;
            }

            bool ngayTraHasBindingError = false;
            if (ModelState.TryGetValue("NgayTra", out var ntEntry) && ntEntry.Errors.Count > 0)
            {
                ntEntry.Errors.Clear();
                ModelState.AddModelError("NgayTra", "Ngày trả không hợp lệ.");
                isAvailabilityFilterValid = false;
                ngayTraHasBindingError = true;
            }

            if (!ngayNhanHasBindingError && !ngayTraHasBindingError)
            {
                if (ngayNhan.HasValue && !ngayTra.HasValue)
                {
                    ModelState.AddModelError("NgayTra", "Vui lòng nhập ngày trả.");
                    isAvailabilityFilterValid = false;
                }
                else if (!ngayNhan.HasValue && ngayTra.HasValue)
                {
                    ModelState.AddModelError("NgayNhan", "Vui lòng nhập ngày nhận.");
                    isAvailabilityFilterValid = false;
                }
                else if (ngayNhan.HasValue && ngayTra.HasValue && ngayNhan.Value.Date >= ngayTra.Value.Date)
                {
                    ModelState.AddModelError("NgayTra", "Ngày trả phải sau ngày nhận.");
                    isAvailabilityFilterValid = false;
                }
            }

            if (!isPriceFilterValid)
            {
                giaMin = null;
                giaMax = null;
            }
            if (!isAvailabilityFilterValid)
            {
                ngayNhan = null;
                ngayTra = null;
            }

            sortOrder = string.IsNullOrEmpty(sortOrder) ? "soPhong_asc" : sortOrder;

            int totalItems = await _phongRepository.DemTimKiemVaLocFullAsync(keyword, maLoaiPhong, tang, trangThai, isPriceFilterValid ? giaMin : null, isPriceFilterValid ? giaMax : null, isAvailabilityFilterValid ? ngayNhan : null, isAvailabilityFilterValid ? ngayTra : null);

            int pageSize = new PhongListViewModel().PageSize;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages == 0)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                page = totalPages;
            }
            else if (page < 1)
            {
                page = 1;
            }

            var phongs = await _phongRepository.TimKiemVaLocFullAsync(keyword, maLoaiPhong, tang, trangThai, isPriceFilterValid ? giaMin : null, isPriceFilterValid ? giaMax : null, isAvailabilityFilterValid ? ngayNhan : null, isAvailabilityFilterValid ? ngayTra : null, sortOrder, (page - 1) * pageSize, pageSize);

            var loaiPhongs = await _loaiPhongRepository.LayTatCaAsync();
            var trangThaiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Trong", Text = TrangThaiHelper.HienThi("Trong") },
                new SelectListItem { Value = "DangXuLy", Text = TrangThaiHelper.HienThi("DangXuLy") },
                new SelectListItem { Value = "DaDat", Text = TrangThaiHelper.HienThi("DaDat") },
                new SelectListItem { Value = "DangSuDung", Text = TrangThaiHelper.HienThi("DangSuDung") },
                new SelectListItem { Value = "BaoTri", Text = TrangThaiHelper.HienThi("BaoTri") }
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
                NgayNhan = ngayNhan,
                NgayTra = ngayTra,
                CurrentPage = page,
                
                TotalItems = totalItems,
                SortOrder = sortOrder,
                LoaiPhongList = new SelectList(loaiPhongs, "MaLoaiPhong", "TenLoai", maLoaiPhong),
                TrangThaiList = new SelectList(trangThaiList, "Value", "Text", trangThai)
            };
            return View(viewModel);
        }

        // GET: Phong/Details/5
        // Chi tiết phòng quản lý chỉ dành cho Admin và Nhân viên.
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phong = await _phongRepository.LayTheoMaAsync(id.Value);

            if (phong == null)
            {
                return NotFound();
            }

            return View(phong);
        }

        private void PopulateDropdowns(int? selectedLoaiPhong = null, string? selectedTrangThai = null)
        {
            var loaiPhongs = _loaiPhongRepository.LayTatCaAsync().GetAwaiter().GetResult();
            ViewBag.MaLoaiPhong = new SelectList(loaiPhongs, "MaLoaiPhong", "TenLoai", selectedLoaiPhong);

            var trangThaiList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Trong", Text = TrangThaiHelper.HienThi("Trong") },
                new SelectListItem { Value = "DangXuLy", Text = TrangThaiHelper.HienThi("DangXuLy") },
                new SelectListItem { Value = "DaDat", Text = TrangThaiHelper.HienThi("DaDat") },
                new SelectListItem { Value = "DangSuDung", Text = TrangThaiHelper.HienThi("DangSuDung") },
                new SelectListItem { Value = "BaoTri", Text = TrangThaiHelper.HienThi("BaoTri") }
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
                bool isDuplicate = await _phongRepository.SoPhongDaTonTaiAsync(phong.SoPhong);
                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Phong.SoPhong), "Số phòng đã tồn tại.");
                }
                else
                {
                    await _phongRepository.ThemAsync(phong);
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

            var phong = await _phongRepository.LayTheoMaCoLoaiPhongAsync(id.Value);
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
                bool isDuplicate = await _phongRepository.SoPhongDaTonTaiAsync(phong.SoPhong, phong.MaPhong);
                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Phong.SoPhong), "Số phòng đã tồn tại.");
                }
                else
                {
                    try
                    {
                        await _phongRepository.CapNhatAsync(phong);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (await _phongRepository.LayTheoMaAsync(phong.MaPhong) == null)
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

            var phong = await _phongRepository.LayTheoMaAsync(id.Value);

            if (phong == null)
            {
                return NotFound();
            }

            bool hasHistory = await _phongRepository.CoLichSuDatPhongAsync(id.Value);
            ViewBag.HasHistory = hasHistory;

            return View(phong);
        }

        // POST: Phong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "NhanVien")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasHistory = await _phongRepository.CoLichSuDatPhongAsync(id);
            if (hasHistory)
            {
                TempData["ErrorMessage"] = "Không thể xóa phòng vì phòng đã phát sinh lịch sử đặt phòng.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            var phong = await _phongRepository.LayTheoMaAsync(id);
            if (phong != null)
            {
                try
                {
                    await _phongRepository.XoaAsync(phong);
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Không thể xóa phòng vì dữ liệu liên quan đã phát sinh hoặc vừa được cập nhật.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}