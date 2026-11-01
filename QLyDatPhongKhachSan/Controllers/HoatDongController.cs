// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung: API chuông thông báo cho Admin/Nhân viên (polling từ _AdminLayout).

using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Services;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin", "NhanVien")]
    public class HoatDongController : Controller
    {
        private readonly IHoatDongService _hoatDongService;

        public HoatDongController(IHoatDongService hoatDongService)
        {
            _hoatDongService = hoatDongService;
        }

        // GET: /HoatDong/ThongBao — 8 thông báo chưa đọc mới nhất + tổng số.
        [HttpGet]
        public async Task<IActionResult> ThongBao()
        {
            var ds = await _hoatDongService.LayThongBaoAsync(8);
            return Ok(new
            {
                soLuong = await _hoatDongService.DemChuaDocAsync(),
                danhSach = ds.Select(h => new
                {
                    maHoatDong = h.MaHoatDong,
                    thoiGian = h.ThoiGian.ToString("dd/MM HH:mm"),
                    loai = h.LoaiHoatDong,
                    moTa = h.MoTa,
                    maLienQuan = h.MaLienQuan
                })
            });
        }

        // POST: /HoatDong/DanhDauDaDoc — khách bấm chuông thì đánh dấu đã đọc.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhDauDaDoc()
        {
            await _hoatDongService.DanhDauDaDocAsync();
            return Ok(new { ok = true });
        }
    }
}
