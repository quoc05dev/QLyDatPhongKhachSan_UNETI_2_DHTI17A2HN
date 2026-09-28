using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 2 - SINH VIÊN 2: QUẢN LÝ PHÒNG, TÌM KIẾM, LỌC, SẮP XẾP, PHÂN TRANG
    // ==========================================
    public class PhongController : Controller
    {
        private readonly AppDbContext _context;

        public PhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Phong (Hỗ trợ Search, Filter, Sort, Pagination)
        public IActionResult Index(string? keyword, int? maLoaiPhong, int? tang, string? trangThai, decimal? giaMin, decimal? giaMax, string? sortOrder, int page = 1)
        {
            // TODO: Sinh viên 2 thực hiện truy vấn LINQ với Search, Filter, Sort, Skip/Take Phân trang
            return View();
        }

        // GET: Phong/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // GET: Phong/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Phong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.Phong phong)
        {
            return View(phong);
        }

        // GET: Phong/Edit/5
        public IActionResult Edit(int? id)
        {
            return View();
        }

        // POST: Phong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Models.Phong phong)
        {
            return View(phong);
        }

        // GET: Phong/Delete/5
        public IActionResult Delete(int? id)
        {
            return View();
        }

        // POST: Phong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
