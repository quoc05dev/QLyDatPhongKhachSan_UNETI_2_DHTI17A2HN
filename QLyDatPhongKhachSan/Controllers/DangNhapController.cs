using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 1 - SINH VIÊN 1: ĐĂNG NHẬP / ĐĂNG KÝ / ĐĂNG XUẤT
    // ==========================================
    public class DangNhapController : Controller
    {
        private readonly AppDbContext _context;

        public DangNhapController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /DangNhap/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /DangNhap/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string tenDangNhap, string matKhau)
        {
            // TODO: Sinh viên 1 thực hiện logic kiểm tra đăng nhập & lưu Session
            return View();
        }

        // GET: /DangNhap/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /DangNhap/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Models.TaiKhoan taiKhoan)
        {
            // TODO: Sinh viên 1 thực hiện logic đăng ký
            return View();
        }

        // GET: /DangNhap/Logout
        public IActionResult Logout()
        {
            // TODO: Xóa Session và chuyển hướng
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: /DangNhap/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
