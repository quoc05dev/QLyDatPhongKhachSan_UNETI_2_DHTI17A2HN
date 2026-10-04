// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Xử lý Đăng nhập, Đăng ký, Đăng xuất và Phân quyền AccessDenied bằng LINQ & EF Core.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class DangNhapController : Controller
    {
        private readonly AppDbContext _context;

        public DangNhapController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /DangNhap/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                var role = HttpContext.Session.GetVaiTro();
                if (role == "Admin") return RedirectToAction("Index", "TaiKhoan");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /DangNhap/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // LINQ kiểm tra tài khoản trong Database
            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == model.TenDangNhap && t.MatKhau == model.MatKhau);

            // 1. Kiểm tra thông tin sai
            if (taiKhoan == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // 2. Kiểm tra tài khoản bị khóa
            if (!taiKhoan.TrangThai)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên.");
                return View(model);
            }

            // 3. Lưu thông tin vào Session khi đăng nhập thành công
            HttpContext.Session.SetUserSession(taiKhoan.MaTaiKhoan, taiKhoan.TenDangNhap, taiKhoan.HoTen, taiKhoan.VaiTro);

            TempData["SuccessMessage"] = $"Chào mừng {taiKhoan.HoTen} đã đăng nhập thành công!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Phân hướng giao diện theo Vai Trò
            if (taiKhoan.VaiTro == "Admin")
            {
                return RedirectToAction("Index", "TaiKhoan");
            }
            else if (taiKhoan.VaiTro == "NhanVien")
            {
                return RedirectToAction("Index", "Phong");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /DangNhap/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /DangNhap/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng Tên Đăng Nhập
            bool isTenDangNhapExists = await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (isTenDangNhapExists)
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                return View(model);
            }

            // Kiểm tra trùng Email
            bool isEmailExists = await _context.TaiKhoans.AnyAsync(t => t.Email == model.Email);
            if (isEmailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng cho một tài khoản khác.");
                return View(model);
            }

            // Tạo Entity TaiKhoan mới
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau,
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                VaiTro = "KhachHang",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoans.Add(taiKhoan);
            await _context.SaveChangesAsync();

            // Tự động tạo bản ghi KhachHang tương ứng kết nối qua MaTaiKhoan
            var khachHang = new KhachHang
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai ?? string.Empty,
                CCCD = model.CCCD,
                TrangThai = true,
                GhiChu = "Đăng ký từ hệ thống online"
            };

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

        // GET: /DangNhap/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.ClearUserSession();
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
            return RedirectToAction("Login", "DangNhap");
        }

        // GET: /DangNhap/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
