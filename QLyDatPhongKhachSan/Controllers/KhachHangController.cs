using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 3 - SINH VIÊN 3: KHÁCH HÀNG & HỒ SƠ CÁ NHÂN
    // ==========================================
    public class KhachHangController : Controller
    {
        private readonly AppDbContext _context;

        public KhachHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: KhachHang (Danh sách khách hàng cho Admin/Nhân viên)
        public IActionResult Index()
        {
            return View();
        }

        // GET: KhachHang/Profile (Xem & sửa hồ sơ cá nhân của khách)
        public IActionResult Profile()
        {
            return View();
        }

        // POST: KhachHang/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(Models.KhachHang khachHang)
        {
            return View(khachHang);
        }

        // GET: KhachHang/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // GET: KhachHang/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.KhachHang khachHang)
        {
            return View(khachHang);
        }

        // GET: KhachHang/Edit/5
        public IActionResult Edit(int? id)
        {
            return View();
        }

        // POST: KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Models.KhachHang khachHang)
        {
            return View(khachHang);
        }
    }
}
