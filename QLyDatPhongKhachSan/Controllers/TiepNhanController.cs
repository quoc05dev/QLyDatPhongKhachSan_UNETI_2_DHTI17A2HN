using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 4 - SINH VIÊN 4: TIẾP NHẬN, NHẬN PHÒNG, TRẢ PHÒNG & QUẢN LÝ TRẠNG THÁI
    // ==========================================
    public class TiepNhanController : Controller
    {
        private readonly AppDbContext _context;

        public TiepNhanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TiepNhan (Danh sách đơn đặt phòng cần xử lý)
        public IActionResult Index(string? searchTerm, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            // TODO: Sinh viên 4 lấy danh sách lọc theo trạng thái (ChoXuLy, DangXuLy, DangSuDung...)
            return View();
        }

        // POST: TiepNhan/Accept/5 (Tiếp nhận đơn: Chờ xử lý -> Đang xử lý)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(int id)
        {
            // TODO: Sinh viên 4 cập nhật trạng thái đơn sang 'DangXuLy'
            return RedirectToAction(nameof(Index));
        }

        // POST: TiepNhan/CheckIn/5 (Nhận phòng: Đang xử lý -> Đã nhận phòng / Đang sử dụng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(int id)
        {
            // TODO: Sinh viên 4 cập nhật trạng thái đơn & phòng
            return RedirectToAction(nameof(Index));
        }

        // POST: TiepNhan/CheckOut/5 (Trả phòng: Đang sử dụng -> Hoàn thành)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOut(int id)
        {
            // TODO: Sinh viên 4 tính tiền, hoàn tất đơn & chuyển phòng về 'Trong'
            return RedirectToAction(nameof(Index));
        }

        // POST: TiepNhan/Reject/5 (Từ chối / Hủy đơn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string lyDo)
        {
            // TODO: Sinh viên 4 xử lý từ chối đơn
            return RedirectToAction(nameof(Index));
        }
    }
}
