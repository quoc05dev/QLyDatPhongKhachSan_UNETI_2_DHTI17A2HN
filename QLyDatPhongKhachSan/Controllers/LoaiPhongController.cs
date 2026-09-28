using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 1 - SINH VIÊN 1: QUẢN LÝ LOẠI PHÒNG
    // ==========================================
    public class LoaiPhongController : Controller
    {
        private readonly AppDbContext _context;

        public LoaiPhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: LoaiPhong
        public IActionResult Index()
        {
            return View();
        }

        // GET: LoaiPhong/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // GET: LoaiPhong/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: LoaiPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.LoaiPhong loaiPhong)
        {
            return View(loaiPhong);
        }

        // GET: LoaiPhong/Edit/5
        public IActionResult Edit(int? id)
        {
            return View();
        }

        // POST: LoaiPhong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Models.LoaiPhong loaiPhong)
        {
            return View(loaiPhong);
        }

        // GET: LoaiPhong/Delete/5
        public IActionResult Delete(int? id)
        {
            return View();
        }

        // POST: LoaiPhong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
