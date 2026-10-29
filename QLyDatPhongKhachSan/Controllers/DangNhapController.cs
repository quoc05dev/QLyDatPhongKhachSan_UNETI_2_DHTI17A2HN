// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Xử lý Đăng nhập, Đăng ký, Đăng xuất và Phân quyền AccessDenied bằng LINQ & EF Core.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class DangNhapController : Controller
    {
        private readonly ITaiKhoanRepository _taiKhoanRepository;
        private readonly IKhachHangRepository _khachHangRepository;
        private readonly IConfiguration _configuration;

        public DangNhapController(
            ITaiKhoanRepository taiKhoanRepository,
            IKhachHangRepository khachHangRepository,
            IConfiguration configuration)
        {
            _taiKhoanRepository = taiKhoanRepository;
            _khachHangRepository = khachHangRepository;
            _configuration = configuration;
        }

        private bool GoogleEnabled =>
            !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);

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
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm theo tên đăng nhập, sau đó kiểm tra mật khẩu bằng PBKDF2.
            // Không so sánh trực tiếp trong LINQ vì mật khẩu được lưu dạng băm.
            var taiKhoan = await _taiKhoanRepository.LayTheoTenDangNhapAsync(model.TenDangNhap);

            // 1. Kiểm tra thông tin sai
            if (taiKhoan == null || !MatKhauHelper.Verify(taiKhoan.MatKhau, model.MatKhau))
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
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng Tên Đăng Nhập
            bool isTenDangNhapExists = await _taiKhoanRepository.TenDangNhapDaTonTaiAsync(model.TenDangNhap);
            if (isTenDangNhapExists)
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                return View(model);
            }

            // Kiểm tra trùng Email
            bool isEmailExists = await _taiKhoanRepository.EmailDaTonTaiAsync(model.Email);
            if (isEmailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng cho một tài khoản khác.");
                return View(model);
            }

            // Tạo Entity TaiKhoan mới
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = MatKhauHelper.Hash(model.MatKhau),
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                VaiTro = "KhachHang",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            await _taiKhoanRepository.ThemAsync(taiKhoan);

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

            await _khachHangRepository.ThemAsync(khachHang);

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

        // GET: /DangNhap/GoogleLogin
        // Chuyển hướng sang trang chọn tài khoản Google (OAuth 2.0).
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            if (!GoogleEnabled)
            {
                TempData["ErrorMessage"] = "Đăng nhập bằng Google chưa được cấu hình. Vui lòng liên hệ quản trị viên.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(GoogleCallback), "DangNhap", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /dang-nhap/google-callback
        // Google trả kết quả về đây: đăng nhập nếu email đã có tài khoản, ngược lại tự tạo tài khoản Khách hàng.
        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                TempData["ErrorMessage"] = $"Đăng nhập Google không thành công: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!auth.Succeeded || auth.Principal == null)
            {
                TempData["ErrorMessage"] = "Không đọc được thông tin đăng nhập Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }

            // Đã lấy được thông tin -> xoá cookie tạm của luồng external login.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var email = (auth.Principal.FindFirstValue(ClaimTypes.Email)
                         ?? auth.Principal.FindFirstValue("email"))?.Trim();
            var hoTen = auth.Principal.FindFirstValue(ClaimTypes.Name)
                        ?? auth.Principal.FindFirstValue("name");

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Tài khoản Google không cung cấp email nên không thể đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            hoTen = string.IsNullOrWhiteSpace(hoTen) ? email : hoTen.Trim();

            // Google chỉ được coi là đã xác minh email khi claim email_verified = true.
            // Nếu không kiểm tra, kẻ tấn công có thể tạo tài khoản Google bằng email
            // của người khác rồi chiếm tài khoản cục bộ trùng email (pre-hijacking).
            var emailVerified = auth.Principal.FindFirstValue("email_verified");
            if (!string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Email Google chưa được xác minh nên không thể đăng nhập. Vui lòng xác minh email trong tài khoản Google.";
                return RedirectToAction(nameof(Login));
            }

            var googleSubject = auth.Principal.FindFirstValue("sub")
                                ?? auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(googleSubject))
            {
                TempData["ErrorMessage"] = "Không đọc được mã định danh tài khoản Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }

            // 1. Ưu tiên tìm theo Google subject (ổn định kể cả khi email đổi).
            var taiKhoan = await _taiKhoanRepository.LayTheoGoogleSubjectAsync(googleSubject);
            var laTaiKhoanMoi = false;

            if (taiKhoan == null)
            {
                var taiKhoanTheoEmail = await _taiKhoanRepository.LayTheoEmailAsync(email);

                if (taiKhoanTheoEmail != null)
                {
                    // Không tự liên kết Google vào tài khoản đã có mật khẩu cục bộ.
                    if (!taiKhoanTheoEmail.LaTaiKhoanGoogle)
                    {
                        TempData["ErrorMessage"] = "Email này đã có tài khoản đăng nhập bằng mật khẩu. Vui lòng đăng nhập bằng mật khẩu hoặc liên hệ quản trị viên.";
                        return RedirectToAction(nameof(Login));
                    }

                    if (!string.IsNullOrEmpty(taiKhoanTheoEmail.GoogleSubject) &&
                        !string.Equals(taiKhoanTheoEmail.GoogleSubject, googleSubject, StringComparison.Ordinal))
                    {
                        TempData["ErrorMessage"] = "Email này đã liên kết với một tài khoản Google khác. Vui lòng liên hệ quản trị viên.";
                        return RedirectToAction(nameof(Login));
                    }

                    taiKhoanTheoEmail.GoogleSubject = googleSubject;
                    await _taiKhoanRepository.CapNhatAsync(taiKhoanTheoEmail);
                    taiKhoan = taiKhoanTheoEmail;
                }
            }

            if (taiKhoan == null)
            {
                laTaiKhoanMoi = true;
                taiKhoan = new TaiKhoan
                {
                    TenDangNhap = await TaoTenDangNhapDuyNhatAsync(email),
                    // Tài khoản Google không dùng mật khẩu form; lưu hash ngẫu nhiên để không thể đăng nhập bằng mật khẩu.
                    MatKhau = MatKhauHelper.Hash(Guid.NewGuid().ToString("N")),
                    HoTen = hoTen,
                    Email = email,
                    VaiTro = "KhachHang",
                    TrangThai = true,
                    NgayTao = DateTime.Now,
                    GoogleSubject = googleSubject,
                    LaTaiKhoanGoogle = true
                };

                try
                {
                    await _taiKhoanRepository.ThemAsync(taiKhoan);
                }
                catch (DbUpdateException)
                {
                    // Hai callback đồng thời cho cùng một Google subject: request
                    // còn lại đã tạo trước, đọc lại bản ghi đó thay vì báo lỗi.
                    taiKhoan = await _taiKhoanRepository.LayTheoGoogleSubjectAsync(googleSubject);
                    laTaiKhoanMoi = false;
                    if (taiKhoan == null)
                    {
                        throw;
                    }
                }
            }

            if (!taiKhoan.LaTaiKhoanGoogle)
            {
                TempData["ErrorMessage"] = "Tài khoản này chưa được liên kết với Google. Vui lòng đăng nhập bằng mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            // 2. Chỉ cho phép tài khoản Khách hàng đăng nhập bằng Google.
            if (!string.Equals(taiKhoan.VaiTro, "KhachHang", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Tài khoản này không được phép đăng nhập bằng Google.";
                return RedirectToAction(nameof(Login));
            }

            if (!taiKhoan.TrangThai)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên.";
                return RedirectToAction(nameof(Login));
            }

            // 3. Đồng bộ hồ sơ Khách hàng (ưu tiên bản ghi đã có theo MaTaiKhoan hoặc cùng email).
            var khachHang = await _khachHangRepository.LayTheoMaTaiKhoanAsync(taiKhoan.MaTaiKhoan);
            if (khachHang == null)
            {
                var khachHangTheoEmail = await _khachHangRepository.LayTheoEmailAsync(email);
                if (khachHangTheoEmail != null && khachHangTheoEmail.MaTaiKhoan == null)
                {
                    khachHangTheoEmail.MaTaiKhoan = taiKhoan.MaTaiKhoan;
                    if (string.IsNullOrWhiteSpace(khachHangTheoEmail.HoTen))
                    {
                        khachHangTheoEmail.HoTen = hoTen;
                    }
                    await _khachHangRepository.CapNhatAsync(khachHangTheoEmail);
                    khachHang = khachHangTheoEmail;
                }
                else if (khachHangTheoEmail != null && khachHangTheoEmail.MaTaiKhoan == taiKhoan.MaTaiKhoan)
                {
                    khachHang = khachHangTheoEmail;
                }
                else
                {
                    khachHang = new KhachHang
                    {
                        MaTaiKhoan = taiKhoan.MaTaiKhoan,
                        HoTen = hoTen,
                        Email = email,
                        SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty,
                        CCCD = string.Empty,
                        TrangThai = true,
                        GhiChu = "Đăng ký bằng Google"
                    };
                    await _khachHangRepository.ThemAsync(khachHang);
                }
            }

            HttpContext.Session.SetUserSession(taiKhoan.MaTaiKhoan, taiKhoan.TenDangNhap, taiKhoan.HoTen, taiKhoan.VaiTro);

            TempData["SuccessMessage"] = laTaiKhoanMoi
                ? $"Đăng ký bằng Google thành công! Chào mừng {taiKhoan.HoTen}."
                : $"Chào mừng {taiKhoan.HoTen} đã đăng nhập bằng Google!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // Sinh tên đăng nhập không trùng từ phần trước @ của email Google.
        private async Task<string> TaoTenDangNhapDuyNhatAsync(string email)
        {
            var tenCoBan = email.Split('@')[0].Trim();
            if (tenCoBan.Length < 3)
            {
                tenCoBan = $"google{tenCoBan}";
            }

            var ungVien = tenCoBan.Length > 50 ? tenCoBan[..50] : tenCoBan;
            var thuTu = 0;
            while (await _taiKhoanRepository.TenDangNhapDaTonTaiAsync(ungVien))
            {
                thuTu++;
                var hauTo = thuTu.ToString();
                var toiDa = 50 - hauTo.Length;
                var goc = tenCoBan.Length > toiDa ? tenCoBan[..toiDa] : tenCoBan;
                ungVien = goc + hauTo;
            }

            return ungVien;
        }

        // GET: /DangNhap/DoiMatKhau
        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            if (!HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction(nameof(Login));
            }
            return View(new DoiMatKhauViewModel());
        }

        // POST: /DangNhap/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> DoiMatKhau(DoiMatKhauViewModel model)
        {
            if (!HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var maTaiKhoan = HttpContext.Session.GetMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var taiKhoan = await _taiKhoanRepository.LayTheoMaAsync(maTaiKhoan.Value);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Kiểm tra mật khẩu hiện tại trước khi cho đổi
            if (!MatKhauHelper.Verify(taiKhoan.MatKhau, model.MatKhauHienTai))
            {
                ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // Không cho đổi sang mật khẩu mới trùng mật khẩu cũ
            if (MatKhauHelper.Verify(taiKhoan.MatKhau, model.MatKhauMoi))
            {
                ModelState.AddModelError("MatKhauMoi", "Mật khẩu mới không được trùng mật khẩu hiện tại.");
                return View(model);
            }

            taiKhoan.MatKhau = MatKhauHelper.Hash(model.MatKhauMoi);
            await _taiKhoanRepository.CapNhatAsync(taiKhoan);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /DangNhap/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.ClearUserSession();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
            return RedirectToAction("Login");
        }

        // GET: /DangNhap/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
