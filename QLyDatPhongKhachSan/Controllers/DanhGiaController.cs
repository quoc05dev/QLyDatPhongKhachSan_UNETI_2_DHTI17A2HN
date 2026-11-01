// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung: Đánh giá khách hàng + AI phân tích cảm xúc & gợi ý phản hồi.

using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Services;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class DanhGiaController : Controller
    {
        private readonly IDanhGiaService _danhGiaService;

        public DanhGiaController(IDanhGiaService danhGiaService)
        {
            _danhGiaService = danhGiaService;
        }

        // ==================== KHÁCH HÀNG ====================

        // GET: DanhGia/Tao?maDatPhong=123 — form đánh giá cho đơn đã hoàn thành.
        [HttpGet]
        public async Task<IActionResult> Tao(int maDatPhong)
        {
            var maTaiKhoan = HttpContext.Session.GetMaTaiKhoan();
            if (maTaiKhoan == null) return RedirectToAction("Login", "DangNhap");

            var daDanhGia = await _danhGiaService.LayTheoMaDatPhongAsync(maDatPhong);
            if (daDanhGia != null)
            {
                TempData["ErrorMessage"] = "Đơn này đã được đánh giá rồi. Cảm ơn bạn!";
                return RedirectToAction("LichSu", "DatPhong");
            }

            ViewBag.MaDatPhong = maDatPhong;
            return View(new DanhGiaCreateViewModel { MaDatPhong = maDatPhong });
        }

        // POST: DanhGia/Tao — lưu đánh giá + AI phân tích cảm xúc + bắn thông báo admin.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tao(DanhGiaCreateViewModel model)
        {
            var maTaiKhoan = HttpContext.Session.GetMaTaiKhoan();
            if (maTaiKhoan == null) return RedirectToAction("Login", "DangNhap");

            if (!ModelState.IsValid)
            {
                ViewBag.MaDatPhong = model.MaDatPhong;
                return View(model);
            }

            var (ketQua, loi) = await _danhGiaService.TaoAsync(model.MaDatPhong, model.SoSao, model.BinhLuan, maTaiKhoan.Value);
            if (ketQua == null)
            {
                ModelState.AddModelError(string.Empty, loi ?? "Không thể lưu đánh giá.");
                ViewBag.MaDatPhong = model.MaDatPhong;
                return View(model);
            }

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá! Khách sạn sẽ phản hồi sớm nhất.";
            return RedirectToAction("LichSu", "DatPhong");
        }

        // ==================== ADMIN / NHÂN VIÊN ====================

        // GET: DanhGia/QuanLy — danh sách đánh giá kèm cảm xúc + nháp phản hồi AI.
        [RoleAuthorize("Admin", "NhanVien")]
        [HttpGet]
        public async Task<IActionResult> QuanLy()
        {
            var danhSach = await _danhGiaService.LayTatCaAsync();
            return View(danhSach);
        }

        // POST: DanhGia/PhanHoi — nhân viên gửi (lưu) phản hồi chính thức cho khách.
        [RoleAuthorize("Admin", "NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanHoi(int maDanhGia, string phanHoi)
        {
            if (string.IsNullOrWhiteSpace(phanHoi))
            {
                TempData["ErrorMessage"] = "Nội dung phản hồi không được để trống.";
            }
            else
            {
                await _danhGiaService.CapNhatPhanHoiAsync(maDanhGia, phanHoi);
                TempData["SuccessMessage"] = "Đã lưu phản hồi cho đánh giá.";
            }
            return RedirectToAction(nameof(QuanLy));
        }
    }
}
