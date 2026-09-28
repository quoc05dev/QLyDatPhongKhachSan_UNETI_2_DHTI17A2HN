using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: DASHBOARD & THỐNG KÊ DOANH THU
    // ==========================================
    public class ThongKeController : Controller
    {
        private readonly AppDbContext _context;

        public ThongKeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ThongKe/Dashboard
        public IActionResult Dashboard()
        {
            // TODO: Sinh viên 5 tính toán tổng số phòng, phòng trống, tỷ lệ sử dụng, doanh thu
            return View();
        }

        // GET: ThongKe/DoanhThu
        public IActionResult DoanhThu(int? nam, int? thang)
        {
            // TODO: Sinh viên 5 dùng LINQ Sum(), GroupBy() thống kê theo tháng/năm/loại phòng
            return View();
        }
    }
}
