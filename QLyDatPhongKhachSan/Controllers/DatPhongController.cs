using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 3 - SINH VIÊN 3: ĐẶT PHÒNG, THEO DÕI & HỦY ĐẶT PHÒNG
    // ==========================================
    public class DatPhongController : Controller
    {
        private readonly AppDbContext _context;

        public DatPhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DatPhong (Lịch sử đặt phòng của khách hàng)
        public IActionResult Index()
        {
            return View();
        }

        // GET: DatPhong/Book?maPhong=5
        public IActionResult Book(int? maPhong)
        {
            return View();
        }

        // POST: DatPhong/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(Models.DatPhong datPhong)
        {
            // TODO: Sinh viên 3 thực hiện kiểm tra ngày, user và lưu đơn đặt phòng
            return View(datPhong);
        }

        // GET: DatPhong/Details/5
        public IActionResult Details(int? id)
        {
            return View();
        }

        // POST: DatPhong/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            // TODO: Sinh viên 3 thực hiện logic hủy phòng nếu trạng thái cho phép
            return RedirectToAction(nameof(Index));
        }
    }
}
