// ==========================================
// Họ và tên: Lê Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - Quản lý khách hàng, hồ sơ cá nhân
// Chức năng: Khách hàng chỉ xem/sửa hồ sơ của chính mình & Chống truy cập trái phép qua URL
// ==========================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly AppDbContext _context;

        public KhachHangController(AppDbContext context)
        {
            _context = context;
        }

        #region Helper Kiểm tra quyền & Session

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("MaTaiKhoan");
        private string? GetCurrentUserRole() => HttpContext.Session.GetString("VaiTro");
        private int? GetCurrentKhachHangId() => HttpContext.Session.GetInt32("MaKhachHang");

        #endregion

        // =====================================================================
        // CHỨC NĂNG CHÍNH CỦA MODULE 3 (SV3):
        // HỒ SƠ CÁ NHÂN — KHÁCH HÀNG CHỈ XEM/SỬA HỒ SƠ CỦA CHÍNH MÌNH
        // =====================================================================

        // GET: KhachHang/Profile (Xem hồ sơ cá nhân của người đang đăng nhập)
        public async Task<IActionResult> Profile()
        {
            var maTaiKhoan = GetCurrentUserId();
            if (maTaiKhoan == null)
            {
                // Chưa đăng nhập -> Chuyển về trang đăng nhập và lưu lại returnUrl
                return RedirectToAction("Login", "DangNhap", new { returnUrl = "/KhachHang/Profile" });
            }

            // Lấy thông tin tài khoản kèm hồ sơ khách hàng bằng LINQ qua EF Core
            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan.Value);

            if (taiKhoan == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "DangNhap");
            }

            // Nếu tài khoản khách hàng chưa có bản ghi trong bảng KhachHang -> Khởi tạo tự động
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

                // Cập nhật lại MaKhachHang vào Session
                HttpContext.Session.SetInt32("MaKhachHang", khachHang.MaKhachHang);
            }

            // Map dữ liệu sang ViewModel để hiển thị trên Razor View
            var viewModel = new HoSoCaNhanViewModel
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TenDangNhap = taiKhoan.TenDangNhap,
                VaiTro = taiKhoan.VaiTro,
                NgayTaoTaiKhoan = taiKhoan.NgayTao,
                HoTen = khachHang.HoTen,
                NgaySinh = khachHang.NgaySinh,
                GioiTinh = khachHang.GioiTinh ?? "Nam",
                CCCD = khachHang.CCCD,
                SoDienThoai = khachHang.SoDienThoai,
                Email = khachHang.Email,
                DiaChi = khachHang.DiaChi,
                QuocTich = khachHang.QuocTich,
                GhiChu = khachHang.GhiChu
            };

            return View(viewModel);
        }

        // POST: KhachHang/Profile (Cập nhật hồ sơ cá nhân của chính mình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(HoSoCaNhanViewModel model)
        {
            var maTaiKhoan = GetCurrentUserId();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // BẢO MẬT: Luôn lấy khách hàng theo MaTaiKhoan trong Session, không tin cậy dữ liệu ID gửi lên từ form
            var khachHang = await _context.KhachHangs
                .Include(kh => kh.TaiKhoan)
                .FirstOrDefaultAsync(kh => kh.MaTaiKhoan == maTaiKhoan.Value);

            if (khachHang == null)
            {
                return NotFound("Không tìm thấy thông tin hồ sơ khách hàng.");
            }

            // Kiểm tra tính hợp lệ nghiệp vụ bổ sung (Mục 7.1 & 13)
            // 1. Ngày sinh không được lớn hơn ngày hiện tại
            if (model.NgaySinh.HasValue && model.NgaySinh.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ (không được lớn hơn ngày hiện tại).");
            }

            // 2. Kiểm tra CCCD không trùng với khách hàng khác trong CSDL
            if (!string.IsNullOrWhiteSpace(model.CCCD))
            {
                bool cccdBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.CCCD == model.CCCD.Trim() && kh.MaKhachHang != khachHang.MaKhachHang);

                if (cccdBiTrung)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD/Hộ chiếu này đã được sử dụng bởi khách hàng khác.");
                }
            }

            // 3. Kiểm tra Email không trùng với khách hàng khác (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bool emailBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.Email == model.Email.Trim() && kh.MaKhachHang != khachHang.MaKhachHang);

                if (emailBiTrung)
                {
                    ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký bởi khách hàng khác.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Điền lại các thông tin chỉ đọc
                model.TenDangNhap = khachHang.TaiKhoan?.TenDangNhap ?? string.Empty;
                model.VaiTro = khachHang.TaiKhoan?.VaiTro ?? "KhachHang";
                model.NgayTaoTaiKhoan = khachHang.TaiKhoan?.NgayTao;
                model.MaKhachHang = khachHang.MaKhachHang;
                model.MaTaiKhoan = khachHang.MaTaiKhoan;
                return View(model);
            }

            // Cập nhật thông tin khách hàng
            khachHang.HoTen = model.HoTen.Trim();
            khachHang.NgaySinh = model.NgaySinh;
            khachHang.GioiTinh = model.GioiTinh;
            khachHang.CCCD = model.CCCD.Trim();
            khachHang.SoDienThoai = model.SoDienThoai.Trim();
            khachHang.Email = model.Email?.Trim();
            khachHang.DiaChi = model.DiaChi?.Trim();
            khachHang.QuocTich = string.IsNullOrWhiteSpace(model.QuocTich) ? "Việt Nam" : model.QuocTich.Trim();
            khachHang.GhiChu = model.GhiChu?.Trim();

            // Đồng bộ sang thông tin Tài Khoản tương ứng
            if (khachHang.TaiKhoan != null)
            {
                khachHang.TaiKhoan.HoTen = khachHang.HoTen;
                khachHang.TaiKhoan.Email = khachHang.Email;
                khachHang.TaiKhoan.SoDienThoai = khachHang.SoDienThoai;
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại họ tên hiển thị trong Session
            HttpContext.Session.SetString("HoTen", khachHang.HoTen);

            TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // =====================================================================
        // BẢO MẬT URL (Mục 7.5 & 22.7):
        // KHÁCH HÀNG KHÔNG ĐƯỢC PHÉP TRUY CẬP HỒ SƠ NGƯỜI KHÁC QUA URL
        // =====================================================================

        // GET: KhachHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var maTaiKhoan = GetCurrentUserId();
            var vaiTro = GetCurrentUserRole();

            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            if (id == null)
            {
                return NotFound();
            }

            // BẢO MẬT: Nếu là Khách hàng -> kiểm tra xem có phải hồ sơ của chính mình không
            if (vaiTro == "KhachHang")
            {
                var myKhachHangId = GetCurrentKhachHangId();
                if (myKhachHangId == null || myKhachHangId.Value != id.Value)
                {
                    // Chặn hành vi cố tình thay đổi id trên URL để xem hồ sơ người khác
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền truy cập hồ sơ của khách hàng có mã ID: {id}. Khách hàng chỉ được xem hồ sơ của chính mình!"
                    });
                }

                // Nếu là của chính mình -> Chuyển về Profile
                return RedirectToAction(nameof(Profile));
            }

            // Admin và Nhân viên được xem chi tiết khách hàng
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                .FirstOrDefaultAsync(m => m.MaKhachHang == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // GET: KhachHang/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var maTaiKhoan = GetCurrentUserId();
            var vaiTro = GetCurrentUserRole();

            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            if (id == null)
            {
                return NotFound();
            }

            // BẢO MẬT: Nếu là Khách hàng -> chỉ được sửa hồ sơ của mình qua trang Profile
            if (vaiTro == "KhachHang")
            {
                var myKhachHangId = GetCurrentKhachHangId();
                if (myKhachHangId == null || myKhachHangId.Value != id.Value)
                {
                    // Chặn hành vi thay đổi id trên URL để sửa hồ sơ người khác
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền chỉnh sửa hồ sơ của khách hàng có mã ID: {id}!"
                    });
                }

                return RedirectToAction(nameof(Profile));
            }

            // Admin / Nhân viên được sửa thông tin khách hàng
            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // POST: KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang khachHang)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap", new
                {
                    message = "Khách hàng vui lòng cập nhật thông tin tại trang Hồ sơ cá nhân của mình!"
                });
            }

            if (id != khachHang.MaKhachHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khachHang);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.KhachHangs.AnyAsync(e => e.MaKhachHang == id))
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
            return View(khachHang);
        }

        // GET: KhachHang (Danh sách khách hàng - Chỉ dành cho Admin và Lễ tân)
        public async Task<IActionResult> Index()
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                // Khách hàng không được truy cập danh sách toàn bộ khách hàng
                return RedirectToAction("AccessDenied", "DangNhap", new
                {
                    message = "Khách hàng không có quyền xem danh sách khách hàng của hệ thống!"
                });
            }

            if (GetCurrentUserId() == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            var list = await _context.KhachHangs.Include(k => k.TaiKhoan).ToListAsync();
            return View(list);
        }

        // GET: KhachHang/Create (Dành cho Lễ tân tiếp nhận khách vãng lai)
        public IActionResult Create()
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }
            return View();
        }

        // POST: KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang khachHang)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            if (ModelState.IsValid)
            {
                _context.Add(khachHang);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khachHang);
        }
    }
}
