using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 1 - SINH VIÊN 1: TÀI KHOẢN & ĐĂNG NHẬP
    // ==========================================
    public class TaiKhoanController : Controller
    {
        private readonly AppDbContext _context;

        public TaiKhoanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TaiKhoan
        public IActionResult Index()
        {
            return View();
        }

        // GET: TaiKhoan/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // GET: TaiKhoan/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TaiKhoan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.TaiKhoan taiKhoan)
        {
            return View(taiKhoan);
        }

        // GET: TaiKhoan/Edit/5
        public IActionResult Edit(int? id)
        {
            return View();
        }

        // POST: TaiKhoan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Models.TaiKhoan taiKhoan)
        {
            return View(taiKhoan);
        }

        // GET: TaiKhoan/Delete/5
        public IActionResult Delete(int? id)
        {
            return View();
        }

        // POST: TaiKhoan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
