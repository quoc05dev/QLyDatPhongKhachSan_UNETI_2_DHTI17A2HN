// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Quản lý dịch vụ của khách sạn.

using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: QUẢN LÝ DỊCH VỤ
    // ==========================================
    public class DichVuController : Controller
    {
        private readonly AppDbContext _context;

        public DichVuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DichVu
        public IActionResult Index()
        {
            var danhSach = _context.DichVus
                .OrderByDescending(dv => dv.MaDichVu)
                .ToList();

            return View(danhSach);
        }

        // GET: DichVu/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // GET: DichVu/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DichVu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.DichVu dichVu)
        {
            return View(dichVu);
        }

        // GET: DichVu/Edit/5
        public IActionResult Edit(int? id)
        {
            return View();
        }

        // POST: DichVu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Models.DichVu dichVu)
        {
            return View(dichVu);
        }

        // GET: DichVu/Delete/5
        public IActionResult Delete(int? id)
        {
            return View();
        }

        // POST: DichVu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
