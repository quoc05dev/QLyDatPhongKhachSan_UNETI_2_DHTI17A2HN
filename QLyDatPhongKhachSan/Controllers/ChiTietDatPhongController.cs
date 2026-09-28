using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: TÍNH TIỀN, CHI TIẾT ĐẶT PHÒNG
    // ==========================================
    public class ChiTietDatPhongController : Controller
    {
        private readonly AppDbContext _context;

        public ChiTietDatPhongController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ChiTietDatPhong/Details/5 (Xem chi tiết & hóa đơn tính tiền)
        public IActionResult Details(int? id)
        {
            // TODO: Sinh viên 5 tính tiền: SoNgay * DonGia = ThanhTien
            return View();
        }

        // GET: ChiTietDatPhong/Create?maDatPhong=5
        public IActionResult Create(int? maDatPhong)
        {
            return View();
        }

        // POST: ChiTietDatPhong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.ChiTietDatPhong chiTiet)
        {
            return View(chiTiet);
        }
    }
}
