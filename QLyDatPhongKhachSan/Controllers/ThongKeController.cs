// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Module 5 - Dashboard tổng quan và thống kê doanh thu
// bằng LINQ theo yêu cầu mục 9.3 và 9.4 của De_15.docx.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    // ==========================================
    // MODULE 5 - SINH VIÊN 5: DASHBOARD & THỐNG KÊ DOANH THU
    // ==========================================
    [RoleAuthorize("Admin", "NhanVien")]
    public class ThongKeController : Controller
    {
        private const int SoPhongDatNhieuNhatToiDa = 10;

        private readonly AppDbContext _context;

        public ThongKeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ThongKe
        // Menu trên thanh điều hướng trỏ vào /ThongKe nên action này chuyển hướng
        // về Dashboard cho khớp với tên chức năng trong đề.
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: ThongKe/Dashboard
        // Mục 9.3: các chỉ số tổng quan của khách sạn.
        public IActionResult Dashboard()
        {
            // Lấy danh sách chi tiết có thu tiền thực tế để dùng cho cả Dashboard
            // và báo cáo thống kê. Những chi tiết đã hủy không phát sinh doanh thu.
            var chiTiets = _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(ct => ct.TrangThai != "DaHuy")
                .ToList();

            var datPhongs = _context.DatPhongs
                .AsNoTracking()
                .Where(dp => dp.TrangThai != "DaHuy")
                .ToList();

            var phongs = _context.Phongs.AsNoTracking().ToList();

            var loaiPhongs = _context.LoaiPhongs
                .AsNoTracking()
                .Where(lp => lp.TrangThai)
                .ToList();

            var model = new DashboardViewModel
            {
                TongLoaiPhong = loaiPhongs.Count,
                TongSoPhong = phongs.Count,
                SoPhongTrong = phongs.Count(p => p.TrangThai == "Trong"),
                SoPhongDangSuDung = phongs.Count(p => p.TrangThai == "DangSuDung"),
                SoPhongBaoTri = phongs.Count(p => p.TrangThai == "BaoTri"),
                TongKhachHang = _context.KhachHangs.AsNoTracking().Count(),
                TongDonDatPhong = datPhongs.Count,
                DonChoXuLy = datPhongs.Count(dp => dp.TrangThai == "ChoXuLy"),
                DonDangXuLy = datPhongs.Count(dp => dp.TrangThai == "DangXuLy"),
                DonHoanThanh = datPhongs.Count(dp => dp.TrangThai == "HoanThanh"),
                DonDaHuy = _context.DatPhongs.AsNoTracking().Count(dp => dp.TrangThai == "DaHuy"),
                TongDoanhThu = chiTiets.Sum(ct => ct.ThanhTien),
                DoanhThuThangNay = chiTiets
                    .Where(ct => ct.NgayNhan.Year == DateTime.Now.Year && ct.NgayNhan.Month == DateTime.Now.Month)
                    .Sum(ct => ct.ThanhTien)
            };

            // Tỷ lệ sử dụng phòng: số phòng có khách chia cho tổng số phòng đang
            // có thể cho thuê (bỏ qua phòng đang bảo trì).
            var phongChoThue = phongs.Count(p => p.TrangThai != "BaoTri");

            // Top phòng được đặt nhiều nhất, sắp theo số lượt đặt giảm dần.
            model.PhongDatNhieuNhat = chiTiets
                .GroupBy(ct => new { ct.MaPhong, ct.Phong!.SoPhong, ct.Phong.LoaiPhong!.TenLoai })
                .Select(g => new PhongThongKeItem
                {
                    SoPhong = g.Key.SoPhong,
                    TenLoai = g.Key.TenLoai,
                    SoLuotDat = g.Count(),
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(p => p.SoLuotDat)
                .ThenByDescending(p => p.DoanhThu)
                .Take(SoPhongDatNhieuNhatToiDa)
                .ToList();

            // Doanh thu và số lượt đặt theo từng tháng trong năm hiện tại.
            model.DoanhThuTheoThang = chiTiets
                .Where(ct => ct.NgayNhan.Year == DateTime.Now.Year)
                .GroupBy(ct => new { ct.NgayNhan.Year, ct.NgayNhan.Month })
                .Select(g => new DoanhThuThangItem
                {
                    Nam = g.Key.Year,
                    Thang = g.Key.Month,
                    DoanhThu = g.Sum(ct => ct.ThanhTien),
                    SoLuotDat = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Tỷ lệ phòng theo từng loại, tính trên tổng số phòng đang có thể thuê.
            model.TyLePhongTheoLoai = loaiPhongs
                .Select(lp => new LoaiPhongThongKeItem
                {
                    TenLoai = lp.TenLoai,
                    SoLuongPhong = phongs.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong),
                    TyLe = phongChoThue == 0
                        ? 0
                        : Math.Round((double)phongs.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong && p.TrangThai != "BaoTri") / phongChoThue * 100, 1)
                })
                .Where(x => x.SoLuongPhong > 0)
                .OrderByDescending(x => x.SoLuongPhong)
                .ToList();

            ViewBag.TyLeSuDungPhong = phongChoThue == 0
                ? 0
                : Math.Round((double)phongs.Count(p => p.TrangThai == "DangSuDung") / phongChoThue * 100, 1);

            return View(model);
        }

        // GET: ThongKe/DoanhThu
        // Mục 9.4: thống kê chi tiết bằng LINQ theo tháng, loại phòng và top phòng.
        public IActionResult DoanhThu(int? nam, int? thang)
        {
            int namHienTai = DateTime.Now.Year;
            int thangHienTai = DateTime.Now.Month;

            // Không truyền tham số thì mặc định xem tháng hiện tại.
            int namChon = nam ?? namHienTai;
            int thangChon = thang ?? thangHienTai;

            // Danh sách năm có phát sinh giao dịch để đổ vào ô chọn trong form.
            ViewBag.DanhSachNam = _context.ChiTietDatPhongs
                .AsNoTracking()
                .Select(ct => ct.NgayNhan.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            ViewBag.NamChon = namChon;
            ViewBag.ThangChon = thangChon;

            // Chi tiết trong khoảng tháng/năm được chọn.
            var chiTietsThang = _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(ct => ct.NgayNhan.Year == namChon && ct.NgayNhan.Month == thangChon)
                .ToList();

            // Lưu cả chi tiết đã hủy để thống kê số lượt hủy, nhưng các con số
            // doanh thu bên dưới chỉ cộng những chi tiết còn hiệu lực.
            var chiTietsHieuLuc = chiTietsThang.Where(ct => ct.TrangThai != "DaHuy").ToList();

            ViewBag.DoanhThuKyChon = chiTietsHieuLuc.Sum(ct => ct.ThanhTien);
            ViewBag.SoLuotDatKyChon = chiTietsHieuLuc.Count;
            ViewBag.SoLuotHuyKyChon = chiTietsThang.Count(ct => ct.TrangThai == "DaHuy");

            // Doanh thu theo từng tháng trong năm đang xem.
            ViewBag.DoanhThuTheoThang = _context.ChiTietDatPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                .Where(ct => ct.NgayNhan.Year == namChon && ct.TrangThai != "DaHuy")
                .GroupBy(ct => ct.NgayNhan.Month)
                .Select(g => new DoanhThuThangItem
                {
                    Nam = namChon,
                    Thang = g.Key,
                    DoanhThu = g.Sum(ct => ct.ThanhTien),
                    SoLuotDat = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Số lượt đặt theo tháng, bao gồm cả đơn đã hủy để thấy đủ biến động.
            ViewBag.LuotDatTheoThang = _context.DatPhongs
                .AsNoTracking()
                .Where(dp => dp.NgayDat.Year == namChon)
                .GroupBy(dp => dp.NgayDat.Month)
                .Select(g => new DoanhThuThangItem
                {
                    Nam = namChon,
                    Thang = g.Key,
                    SoLuotDat = g.Count()
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Số phòng theo loại: dùng Any() để loại phòng không còn phòng nào.
            var tatCaLoaiPhong = _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.MaLoaiPhong)
                .ToList();

            var tatCaPhong = _context.Phongs.AsNoTracking().ToList();

            ViewBag.SoPhongTheoLoai = tatCaLoaiPhong
                .Select(lp => new LoaiPhongThongKeItem
                {
                    TenLoai = lp.TenLoai,
                    SoLuongPhong = tatCaPhong.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong)
                })
                .Where(x => x.SoLuongPhong > 0)
                .ToList();

            // Doanh thu theo loại phòng trong kỳ đang xem.
            ViewBag.DoanhThuTheoLoai = chiTietsHieuLuc
                .Where(ct => ct.Phong != null && ct.Phong.LoaiPhong != null)
                .GroupBy(ct => ct.Phong!.LoaiPhong!.TenLoai)
                .Select(g => new LoaiPhongThongKeItem
                {
                    TenLoai = g.Key,
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            // Phòng được đặt nhiều nhất trong kỳ.
            ViewBag.PhongDatNhieuNhat = chiTietsHieuLuc
                .Where(ct => ct.Phong != null)
                .GroupBy(ct => new { ct.MaPhong, ct.Phong!.SoPhong, ct.Phong.LoaiPhong!.TenLoai })
                .Select(g => new PhongThongKeItem
                {
                    SoPhong = g.Key.SoPhong,
                    TenLoai = g.Key.TenLoai,
                    SoLuotDat = g.Count(),
                    DoanhThu = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(p => p.SoLuotDat)
                .ThenByDescending(p => p.DoanhThu)
                .Take(SoPhongDatNhieuNhatToiDa)
                .ToList();

            // Chi tiết từng lượt đặt của kỳ để hiển thị bảng.
            ViewBag.ChiTietKy = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Where(dp => dp.NgayNhan.Year == namChon && dp.NgayNhan.Month == thangChon)
                .OrderByDescending(dp => dp.NgayNhan)
                .ToList();

            return View();
        }
    }
}