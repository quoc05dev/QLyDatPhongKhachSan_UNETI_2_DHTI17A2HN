// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Tính tiền phòng (số ngày ở, thành tiền) cho chi tiết
// đặt phòng và cập nhật tổng tiền của hóa đơn đặt phòng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: TÍNH TIỀN, CHI TIẾT ĐẶT PHÒNG
    // ==========================================
    [RoleAuthorize("Admin", "NhanVien")]
    public class ChiTietDatPhongController : Controller
    {
        private readonly IChiTietDatPhongRepository _chiTietDatPhongRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly IDatPhongRepository _datPhongRepository;

        public ChiTietDatPhongController(IChiTietDatPhongRepository chiTietDatPhongRepository, IPhongRepository phongRepository, IDatPhongRepository datPhongRepository)
        {
            _chiTietDatPhongRepository = chiTietDatPhongRepository;
            _phongRepository = phongRepository;
            _datPhongRepository = datPhongRepository;
        }

        #region Công thức tính tiền

        // Số ngày ở = (Ngày trả - Ngày nhận), tối thiểu 1 ngày
        public static int TinhSoNgay(DateTime ngayNhan, DateTime ngayTra)
        {
            if (ngayNhan == default || ngayTra == default)
            {
                return 1;
            }

            int soNgay = (ngayTra.Date - ngayNhan.Date).Days;

            return soNgay < 1 ? 1 : soNgay;
        }

        // Thành tiền = Số ngày ở x Đơn giá
        public static decimal TinhThanhTien(int soNgay, decimal donGia)
        {
            if (soNgay < 0)
            {
                soNgay = 0;
            }

            if (donGia < 0)
            {
                donGia = 0;
            }

            return Math.Round(soNgay * donGia, 2);
        }

        // Tính lại SoNgay và ThanhTien rồi gán ngược vào model
        public static void ApDungCongThucTinhTien(ChiTietDatPhong chiTiet)
        {
            chiTiet.SoNgay = TinhSoNgay(chiTiet.NgayNhan, chiTiet.NgayTra);
            chiTiet.ThanhTien = TinhThanhTien(chiTiet.SoNgay, chiTiet.DonGia);
        }

        // Tổng tiền của hóa đơn = tổng thành tiền các chi tiết chưa bị hủy
        public static decimal TinhTongTien(IEnumerable<ChiTietDatPhong> chiTiets)
        {
            return chiTiets
                .Where(ct => ct.TrangThai != "DaHuy")
                .Sum(ct => ct.ThanhTien);
        }

        #endregion

        #region Kiểm tra dữ liệu

        // Ngày trả phải sau ngày nhận, ngày nhận không được vượt quá ngày trả
        public static bool KiemTraNgayHopLe(DateTime ngayNhan, DateTime ngayTra, out string loi)
        {
            loi = string.Empty;

            if (ngayNhan == default)
            {
                loi = "Vui lòng chọn ngày nhận phòng";
                return false;
            }

            if (ngayTra == default)
            {
                loi = "Vui lòng chọn ngày trả phòng";
                return false;
            }

            if (ngayTra.Date < ngayNhan.Date)
            {
                loi = "Ngày trả phòng không được trước ngày nhận phòng";
                return false;
            }

            return true;
        }

        #endregion

        // GET: ChiTietDatPhong/Details/5 (Xem chi tiết & hóa đơn tính tiền)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chiTiet = await _chiTietDatPhongRepository.LayTheoMaAsync(id.Value);

            if (chiTiet == null)
            {
                return NotFound();
            }

            // Tính lại để hóa đơn luôn khớp với công thức
            ApDungCongThucTinhTien(chiTiet);

            return View(chiTiet);
        }

        // GET: ChiTietDatPhong/Create?maDatPhong=5
        public async Task<IActionResult> Create(int? maDatPhong)
        {
            // Nạp danh sách phòng có thể chọn (loại bỏ phòng đang bảo trì)
            var phongs = await _phongRepository.LayDanhSachPhongKhongBaoTriAsync();

            ViewBag.DanhSachPhong = new SelectList(phongs, "MaPhong", "SoPhong");

            // Thông tin đặt phòng khi được gọi từ màn hình chi tiết đơn
            if (maDatPhong.HasValue)
            {
                ViewBag.DatPhong = await _datPhongRepository.FindAsync(maDatPhong.Value);
                ViewBag.MaDatPhong = maDatPhong.Value;
            }

            return View(new ChiTietDatPhong
            {
                MaDatPhong = maDatPhong ?? 0,
                NgayNhan = DateTime.Today,
                NgayTra = DateTime.Today.AddDays(1),
                SoNgay = 1,
                TrangThai = "ChoNhan"
            });
        }

        // POST: ChiTietDatPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChiTietDatPhong chiTiet)
        {
            if (!KiemTraNgayHopLe(chiTiet.NgayNhan, chiTiet.NgayTra, out string loi))
            {
                ModelState.AddModelError(string.Empty, loi);
            }

            if (!ModelState.IsValid)
            {
                return View(chiTiet);
            }

            // Tính tiền trước khi lưu, không tin vào giá trị gửi lên từ form
            ApDungCongThucTinhTien(chiTiet);

            await _chiTietDatPhongRepository.ThemAsync(chiTiet);

            // Cập nhật tổng tiền của hóa đơn đặt phòng
            var datPhong = await _datPhongRepository.LayTheoMaVoiChiTietAsync(chiTiet.MaDatPhong);

            if (datPhong != null)
            {
                datPhong.TongTien = TinhTongTien(datPhong.ChiTietDatPhongs);
                await _datPhongRepository.CapNhatAsync(datPhong);
            }

            return RedirectToAction(nameof(Details), new { id = chiTiet.MaChiTiet });
        }
    }
}
