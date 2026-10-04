// ==========================================
// Họ và tên: Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - Đặt phòng, kiểm tra điều kiện LINQ, theo dõi lịch sử và hủy đặt phòng
// ==========================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class DatPhongController : Controller
    {
        private readonly AppDbContext _context;

        public DatPhongController(AppDbContext context)
        {
            _context = context;
        }

        #region Helper: Quản lý Session & Khách hàng

        private int? GetCurrentMaTaiKhoan() => HttpContext.Session.GetMaTaiKhoan();
        private string GetCurrentVaiTro() => HttpContext.Session.GetVaiTro();

        /// <summary>
        /// Lấy hoặc tự động khởi tạo hồ sơ KhachHang tương ứng với tài khoản đang đăng nhập
        /// </summary>
        private async Task<KhachHang?> GetOrCreateCurrentKhachHangAsync()
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null) return null;

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan.Value);

            if (taiKhoan == null) return null;

            var khachHang = taiKhoan.KhachHang;
            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    HoTen = taiKhoan.HoTen,
                    Email = taiKhoan.Email,
                    SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty,
                    CCCD = string.Empty,
                    QuocTich = "Việt Nam",
                    TrangThai = true
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            return khachHang;
        }

        #endregion

        // =====================================================================
        // 1. TẠO ĐẶT PHÒNG (BOOK) — KIỂM TRA ĐIỀU KIỆN NGHIỆP VỤ & TRÙNG LỊCH LINQ
        // =====================================================================

        // GET: DatPhong/Book?maPhong=5
        public async Task<IActionResult> Book(int? maPhong)
        {
            // 1. Kiểm tra đăng nhập
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Book", "DatPhong", new { maPhong }) });
            }

            // 2. Kiểm tra mã phòng hợp lệ
            if (maPhong == null)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn phòng bạn muốn đặt trước khi tiếp tục.";
                return RedirectToAction("Index", "Phong");
            }

            // 3. Truy vấn thông tin phòng và loại phòng
            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong.Value);

            if (phong == null)
            {
                TempData["ErrorMessage"] = "Phòng yêu cầu không tồn tại trong hệ thống.";
                return RedirectToAction("Index", "Phong");
            }

            // 4. Kiểm tra trạng thái phòng: chỉ đặt phòng khi không bảo trì
            if (string.Equals(phong.TrangThai, "BaoTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện đang trong thời gian bảo trì, quý khách vui lòng chọn phòng khác.";
                return RedirectToAction("Index", "Phong");
            }

            // 5. Nạp thông tin khách hàng hiện tại
            var khachHang = await GetOrCreateCurrentKhachHangAsync();

            var viewModel = new DatPhongCreateViewModel
            {
                MaPhong = phong.MaPhong,
                SoPhong = phong.SoPhong,
                TenLoaiPhong = phong.LoaiPhong?.TenLoai ?? "Tiêu chuẩn",
                DonGia = phong.DonGia,
                SoNguoiToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2,
                TienNghi = phong.TienNghi,
                HuongPhong = phong.HuongPhong,
                Tang = phong.Tang,
                HoTenKhachHang = khachHang?.HoTen ?? string.Empty,
                SoDienThoai = khachHang?.SoDienThoai ?? string.Empty,
                CCCD = khachHang?.CCCD ?? string.Empty,
                Email = khachHang?.Email,
                NgayNhan = DateTime.Today,
                NgayTraDuKien = DateTime.Today.AddDays(1),
                SoNguoi = 1,
                TienCoc = 0
            };

            return View(viewModel);
        }

        // POST: DatPhong/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(DatPhongCreateViewModel model)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Book", "DatPhong", new { maPhong = model.MaPhong }) });
            }

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == model.MaPhong);

            if (phong == null)
            {
                TempData["ErrorMessage"] = "Phòng không tồn tại.";
                return RedirectToAction("Index", "Phong");
            }

            // ==========================================
            // KIỂM TRA 5 ĐIỀU KIỆN NGHIỆP VỤ BẮT BUỘC (Mục 7.3 & 13)
            // ==========================================

            // Điều kiện 1: Trạng thái phòng có thể kinh doanh (không bảo trì)
            if (string.Equals(phong.TrangThai, "BaoTri", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Phòng này hiện đang bảo trì, không thể tiếp nhận đặt phòng.");
            }

            // Điều kiện 2: Ngày nhận không được ở trong quá khứ
            if (model.NgayNhan.Date < DateTime.Today)
            {
                ModelState.AddModelError("NgayNhan", "Ngày nhận phòng không được ở trong quá khứ.");
            }

            // Điều kiện 3: Ngày trả phải sau ngày nhận ít nhất 1 ngày
            if (model.NgayTraDuKien.Date <= model.NgayNhan.Date)
            {
                ModelState.AddModelError("NgayTraDuKien", "Ngày trả phòng dự kiến phải sau ngày nhận phòng ít nhất 1 ngày.");
            }

            // Điều kiện 4: Kiểm tra sức chứa tối đa của loại phòng
            int sucChuaToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2;
            if (model.SoNguoi > sucChuaToiDa)
            {
                ModelState.AddModelError("SoNguoi", $"Số lượng khách ({model.SoNguoi} người) vượt quá sức chứa tối đa của phòng ({sucChuaToiDa} người).");
            }

            // Điều kiện 5: KIỂM TRA TRÙNG LỊCH ĐẶT PHÒNG BẰNG LINQ
            // Không cho đặt nếu phòng đã có booking hoạt động trùng khoảng thời gian [NgayNhan, NgayTraDuKien]
            bool biTrungLich = await _context.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .AnyAsync(ct => ct.MaPhong == model.MaPhong
                             && ct.DatPhong != null
                             && ct.DatPhong.TrangThai != "DaHuy"
                             && ct.DatPhong.TrangThai != "HoanThanh"
                             && ct.NgayNhan.Date < model.NgayTraDuKien.Date
                             && ct.NgayTra.Date > model.NgayNhan.Date);

            if (biTrungLich)
            {
                ModelState.AddModelError(string.Empty, $"Phòng {phong.SoPhong} đã có khách đặt trong khoảng thời gian {model.NgayNhan:dd/MM/yyyy} - {model.NgayTraDuKien:dd/MM/yyyy}. Quý khách vui lòng chọn ngày khác hoặc phòng khác.");
            }

            // Nếu không hợp lệ, điền lại thông tin phòng và trả về View
            if (!ModelState.IsValid)
            {
                model.SoPhong = phong.SoPhong;
                model.TenLoaiPhong = phong.LoaiPhong?.TenLoai ?? "Tiêu chuẩn";
                model.DonGia = phong.DonGia;
                model.SoNguoiToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2;
                model.TienNghi = phong.TienNghi;
                model.HuongPhong = phong.HuongPhong;
                model.Tang = phong.Tang;
                return View(model);
            }

            // Lấy hồ sơ khách hàng hiện tại
            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // Cập nhật thông tin khách hàng nếu có bổ sung từ form đặt phòng
            if (string.IsNullOrEmpty(khachHang.CCCD) && !string.IsNullOrEmpty(model.CCCD))
            {
                khachHang.CCCD = model.CCCD.Trim();
            }
            if (string.IsNullOrEmpty(khachHang.SoDienThoai) && !string.IsNullOrEmpty(model.SoDienThoai))
            {
                khachHang.SoDienThoai = model.SoDienThoai.Trim();
            }

            // Tính số ngày ở và tổng tiền dự kiến
            int soNgay = (model.NgayTraDuKien.Date - model.NgayNhan.Date).Days;
            if (soNgay < 1) soNgay = 1;
            decimal tongTien = soNgay * phong.DonGia;
            decimal tienCoc = model.TienCoc > tongTien ? tongTien : Math.Max(0, model.TienCoc);

            // 1. Tạo đơn đặt phòng chính (DatPhong)
            var datPhong = new DatPhong
            {
                MaKhachHang = khachHang.MaKhachHang,
                NgayDat = DateTime.Now,
                NgayNhan = model.NgayNhan.Date,
                NgayTraDuKien = model.NgayTraDuKien.Date,
                SoNguoi = model.SoNguoi,
                TienCoc = tienCoc,
                TongTien = tongTien,
                TrangThai = "ChoXuLy", // Luồng trạng thái ban đầu: Chờ xử lý
                GhiChu = model.GhiChu?.Trim()
            };

            _context.DatPhongs.Add(datPhong);
            await _context.SaveChangesAsync();

            // 2. Tạo chi tiết đặt phòng (ChiTietDatPhong) liên kết với phòng đã chọn
            var chiTiet = new ChiTietDatPhong
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaPhong = phong.MaPhong,
                NgayNhan = model.NgayNhan.Date,
                NgayTra = model.NgayTraDuKien.Date,
                DonGia = phong.DonGia,
                SoNgay = soNgay,
                ThanhTien = tongTien,
                TrangThai = "ChoNhan"
            };

            _context.ChiTietDatPhongs.Add(chiTiet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt phòng {phong.SoPhong} thành công! Đơn đặt phòng #{datPhong.MaDatPhong:D4} đã được gửi tới bộ phận Lễ tân xử lý.";
            return RedirectToAction(nameof(Details), new { id = datPhong.MaDatPhong });
        }

        // =====================================================================
        // 2. THEO DÕI ĐẶT PHÒNG (LỊCH SỬ & CHI TIẾT)
        // =====================================================================

        // GET: DatPhong (Lịch sử đặt phòng của khách hàng hiện tại)
        public async Task<IActionResult> Index(string? trangThaiFilter)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Index", "DatPhong") });
            }

            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            var query = _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .Where(d => d.MaKhachHang == khachHang.MaKhachHang)
                .AsNoTracking();

            // Thống kê nhanh các trạng thái
            int tongDon = await query.CountAsync();
            int donChoXuLy = await query.CountAsync(d => d.TrangThai == "ChoXuLy");
            int donDaXacNhan = await query.CountAsync(d => d.TrangThai == "DaXacNhan" || d.TrangThai == "DangXuLy");
            int donHoanThanh = await query.CountAsync(d => d.TrangThai == "HoanThanh");
            int donDaHuy = await query.CountAsync(d => d.TrangThai == "DaHuy");

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(trangThaiFilter))
            {
                query = query.Where(d => d.TrangThai == trangThaiFilter);
            }

            var list = await query
                .OrderByDescending(d => d.NgayDat)
                .Select(d => new DatPhongItemViewModel
                {
                    MaDatPhong = d.MaDatPhong,
                    MaPhong = d.ChiTietDatPhongs.Select(ct => (int?)ct.MaPhong).FirstOrDefault(),
                    SoPhong = d.ChiTietDatPhongs.Select(ct => ct.Phong != null ? ct.Phong.SoPhong : "").FirstOrDefault() ?? "Chưa gán",
                    TenLoaiPhong = d.ChiTietDatPhongs.Select(ct => ct.Phong != null && ct.Phong.LoaiPhong != null ? ct.Phong.LoaiPhong.TenLoai : "").FirstOrDefault() ?? "",
                    NgayDat = d.NgayDat,
                    NgayNhan = d.NgayNhan,
                    NgayTraDuKien = d.NgayTraDuKien,
                    SoNguoi = d.SoNguoi,
                    SoNgayO = (d.NgayTraDuKien.Date - d.NgayNhan.Date).Days < 1 ? 1 : (d.NgayTraDuKien.Date - d.NgayNhan.Date).Days,
                    TienCoc = d.TienCoc,
                    TongTien = d.TongTien,
                    TrangThai = d.TrangThai,
                    GhiChu = d.GhiChu
                })
                .ToListAsync();

            var viewModel = new DatPhongLichSuViewModel
            {
                DanhSachDon = list,
                TrangThaiFilter = trangThaiFilter,
                TongDon = tongDon,
                DonChoXuLy = donChoXuLy,
                DonDaXacNhan = donDaXacNhan,
                DonHoanThanh = donHoanThanh,
                DonDaHuy = donDaHuy
            };

            return View(viewModel);
        }

        // GET: DatPhong/Details/5 (Chi tiết đơn đặt phòng)
        public async Task<IActionResult> Details(int? id)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Details", "DatPhong", new { id }) });
            }

            if (id == null)
            {
                return NotFound();
            }

            var datPhong = await _context.DatPhongs
                .Include(d => d.KhachHang)
                    .ThenInclude(kh => kh!.TaiKhoan)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id.Value);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đơn đặt phòng yêu cầu.");
            }

            // ==========================================
            // BẢO MẬT URL (Mục 7.5 & 22.7):
            // Khách hàng CHỈ ĐƯỢC XEM đơn đặt phòng của chính mình
            // ==========================================
            var vaiTro = GetCurrentVaiTro();
            if (vaiTro == "KhachHang")
            {
                var khachHang = await GetOrCreateCurrentKhachHangAsync();
                if (khachHang == null || datPhong.MaKhachHang != khachHang.MaKhachHang)
                {
                    TempData["ErrorMessage"] = $"Bạn không có quyền xem thông tin đơn đặt phòng #{id}. Khách hàng chỉ được xem đơn đặt phòng của chính mình!";
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền xem thông tin đơn đặt phòng #{id}. Khách hàng chỉ được xem đơn đặt phòng của chính mình!"
                    });
                }
            }

            var chiTiet = datPhong.ChiTietDatPhongs.FirstOrDefault();
            int soNgay = (datPhong.NgayTraDuKien.Date - datPhong.NgayNhan.Date).Days;
            if (soNgay < 1) soNgay = 1;

            var viewModel = new DatPhongChiTietViewModel
            {
                DatPhong = datPhong,
                ChiTiet = chiTiet,
                Phong = chiTiet?.Phong,
                KhachHang = datPhong.KhachHang,
                SoNgayO = soNgay
            };

            return View(viewModel);
        }

        // =====================================================================
        // 3. HỦY ĐẶT PHÒNG (CANCEL) — KIỂM TRA ĐIỀU KIỆN TRẠNG THÁI & BẢO MẬT URL
        // =====================================================================

        // POST: DatPhong/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đơn đặt phòng.");
            }

            // ==========================================
            // BẢO MẬT URL (Mục 7.5 & 22.7):
            // Khách hàng CHỈ ĐƯỢC HỦY đơn đặt phòng của chính mình
            // ==========================================
            var vaiTro = GetCurrentVaiTro();
            if (vaiTro == "KhachHang")
            {
                var khachHang = await GetOrCreateCurrentKhachHangAsync();
                if (khachHang == null || datPhong.MaKhachHang != khachHang.MaKhachHang)
                {
                    TempData["ErrorMessage"] = $"Bạn không có quyền thao tác hủy đơn đặt phòng #{id} của người khác!";
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền thao tác hủy đơn đặt phòng #{id} của người khác!"
                    });
                }
            }

            // ==========================================
            // KIỂM TRA ĐIỀU KIỆN HỦY (Mục 7.4):
            // Chỉ được hủy trong trạng thái cho phép ("ChoXuLy" hoặc "DangXuLy")
            // Không được hủy khi đã nhận phòng ("DangSuDung") hoặc đã hoàn thành ("HoanThanh")
            // ==========================================
            if (datPhong.TrangThai == "DangSuDung" || datPhong.TrangThai == "HoanThanh")
            {
                TempData["ErrorMessage"] = $"Đơn đặt phòng #{datPhong.MaDatPhong:D4} đang được sử dụng hoặc đã hoàn tất, không thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (datPhong.TrangThai == "DaHuy")
            {
                TempData["ErrorMessage"] = $"Đơn đặt phòng #{datPhong.MaDatPhong:D4} đã được hủy trước đó.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Thực hiện cập nhật trạng thái hủy
            datPhong.TrangThai = "DaHuy";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"[Đã hủy bởi khách hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm}]"
                : $"{datPhong.GhiChu} | [Đã hủy bởi khách hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm}]";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.TrangThai = "DaHuy";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn đặt phòng #{datPhong.MaDatPhong:D4} thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
