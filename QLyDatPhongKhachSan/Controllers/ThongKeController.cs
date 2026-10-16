// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Module 5 - Dashboard tổng quan và thống kê doanh thu
// bằng LINQ theo yêu cầu mục 9.3 và 9.4 của De_15.docx.

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        static ThongKeController()
        {
            // QuestPDF yêu cầu khai báo giấy phép trước khi tạo tài liệu.
            // Bản Community miễn phí cho mục đích học tập / đồ án.
            QuestPDF.Settings.License = LicenseType.Community;
        }

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
                // Tổng số giao dịch theo đề là TẤT CẢ các đơn, kể cả đơn đã hủy
                // (đơn đã hủy được thống kê riêng ở thẻ "Đơn đã hủy" bên dưới).
                TongDonDatPhong = _context.DatPhongs.AsNoTracking().Count(),
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

            // Any(): khách sạn đã có giao dịch chi tiết đặt phòng chưa? Nếu chưa
            // có thì không cần chạy truy vấn gom nhóm, các bảng thống kê sẽ hiện
            // thông báo "chưa có dữ liệu" đúng như thiết kế ở View.
            var coGiaoDich = chiTiets.Any();

            // Top phòng được đặt nhiều nhất, sắp theo số lượt đặt giảm dần.
            model.PhongDatNhieuNhat = coGiaoDich
                ? chiTiets
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
                .ToList()
                : new List<PhongThongKeItem>();

            // Doanh thu và số lượt đặt theo từng tháng trong năm hiện tại.
            model.DoanhThuTheoThang = coGiaoDich
                ? chiTiets
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
                .ToList()
                : new List<DoanhThuThangItem>();

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

            // Tỷ lệ sử dụng phòng riêng cho từng loại (phòng lưu trú / phòng của loại)
            // để vẽ danh sách "Công suất theo hạng phòng" đúng với bản thiết kế.
            model.TyLeSuDungTheoLoai = loaiPhongs
                .Select(lp =>
                {
                    var soPhongLoai = phongs.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong);
                    var dangSuDung = phongs.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong && p.TrangThai == "DangSuDung");
                    return new LoaiPhongOccupancyItem
                    {
                        TenLoai = lp.TenLoai,
                        SoLuongPhong = soPhongLoai,
                        TyLeSuDung = soPhongLoai == 0 ? 0 : Math.Round((double)dangSuDung / soPhongLoai * 100, 0)
                    };
                })
                .Where(x => x.SoLuongPhong > 0)
                .OrderByDescending(x => x.TyLeSuDung)
                .ThenByDescending(x => x.SoLuongPhong)
                .Take(4)
                .ToList();

            // Các đơn đặt phòng mới nhất để hiển thị bảng ở trang tổng quan.
            model.DatPhongGanNhat = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .OrderByDescending(dp => dp.NgayDat)
                .Take(6)
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
            // Không truyền tham số thì mặc định xem tháng hiện tại.
            int namChon = nam ?? DateTime.Now.Year;
            int thangChon = thang ?? DateTime.Now.Month;

            var baoCao = LayBaoCaoDoanhThu(namChon, thangChon);

            ViewBag.DanhSachNam = baoCao.DanhSachNam;
            ViewBag.NamChon = baoCao.Nam;
            ViewBag.ThangChon = baoCao.Thang;
            ViewBag.CoDuLieuKyChon = baoCao.CoDuLieu;
            ViewBag.DoanhThuKyChon = baoCao.DoanhThuKy;
            ViewBag.SoLuotDatKyChon = baoCao.SoLuotDat;
            ViewBag.SoLuotHuyKyChon = baoCao.SoLuotHuy;
            ViewBag.DoanhThuTheoThang = baoCao.DoanhThuTheoThang;
            ViewBag.LuotDatTheoThang = baoCao.LuotDatTheoThang;
            ViewBag.SoPhongTheoLoai = baoCao.SoPhongTheoLoai;
            ViewBag.DoanhThuTheoLoai = baoCao.DoanhThuTheoLoai;
            ViewBag.PhongDatNhieuNhat = baoCao.PhongDatNhieuNhat;
            ViewBag.ChiTietKy = baoCao.ChiTietKy;

            return View();
        }

        // GET: ThongKe/ExportExcel
        // Xuất báo cáo doanh thu của kỳ đang chọn ra tệp Excel (.xlsx).
        public IActionResult ExportExcel(int? nam, int? thang)
        {
            int namChon = nam ?? DateTime.Now.Year;
            int thangChon = thang ?? DateTime.Now.Month;

            var baoCao = LayBaoCaoDoanhThu(namChon, thangChon);
            var noiDung = TaoExcelBaoCao(baoCao);

            return File(
                noiDung,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCaoDoanhThu_{thangChon:00}_{namChon}.xlsx");
        }

        // GET: ThongKe/ExportPdf
        // Xuất báo cáo doanh thu của kỳ đang chọn ra tệp PDF.
        public IActionResult ExportPdf(int? nam, int? thang)
        {
            int namChon = nam ?? DateTime.Now.Year;
            int thangChon = thang ?? DateTime.Now.Month;

            var baoCao = LayBaoCaoDoanhThu(namChon, thangChon);
            var noiDung = TaoPdfBaoCao(baoCao);

            return File(noiDung, "application/pdf", $"BaoCaoDoanhThu_{thangChon:00}_{namChon}.pdf");
        }

        // Gom toàn bộ số liệu của một kỳ (tháng/năm) vào một chỗ để màn hình
        // DoanhThu và hai chức năng xuất báo cáo luôn khớp số liệu với nhau.
        private DoanhThuBaoCaoViewModel LayBaoCaoDoanhThu(int namChon, int thangChon)
        {
            int namHienTai = DateTime.Now.Year;

            var baoCao = new DoanhThuBaoCaoViewModel { Nam = namChon, Thang = thangChon };

            // Any(): có giao dịch nào trong hệ thống không? Nếu chưa có thì đưa năm hiện
            // tại vào ô chọn để người dùng vẫn xem được báo cáo (dù rỗng) thay vì
            // bị ô chọn năm trống hoàn toàn.
            baoCao.DanhSachNam = _context.ChiTietDatPhongs.Any()
                ? _context.ChiTietDatPhongs
                    .AsNoTracking()
                    .Select(ct => ct.NgayNhan.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList()
                : new List<int> { namHienTai };

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

            // Any(): tháng/năm đang chọn có phát sinh giao dịch nào không?
            // Dùng để báo cho người dùng biết chính xác vì sao báo cáo rỗng,
            // thay vì chỉ hiện mấy con số 0 không giải thích được.
            baoCao.CoDuLieu = chiTietsHieuLuc.Any();
            baoCao.DoanhThuKy = chiTietsHieuLuc.Sum(ct => ct.ThanhTien);
            baoCao.SoLuotDat = chiTietsHieuLuc.Count;
            baoCao.SoLuotHuy = chiTietsThang.Count(ct => ct.TrangThai == "DaHuy");

            // Doanh thu theo từng tháng trong năm đang xem.
            baoCao.DoanhThuTheoThang = _context.ChiTietDatPhongs
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
            baoCao.LuotDatTheoThang = _context.DatPhongs
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

            baoCao.SoPhongTheoLoai = tatCaLoaiPhong
                .Select(lp => new LoaiPhongThongKeItem
                {
                    TenLoai = lp.TenLoai,
                    SoLuongPhong = tatCaPhong.Count(p => p.MaLoaiPhong == lp.MaLoaiPhong)
                })
                .Where(x => x.SoLuongPhong > 0)
                .ToList();

            // Doanh thu theo loại phòng trong kỳ đang xem.
            baoCao.DoanhThuTheoLoai = chiTietsHieuLuc
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
            baoCao.PhongDatNhieuNhat = chiTietsHieuLuc
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
            baoCao.ChiTietKy = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Where(dp => dp.NgayNhan.Year == namChon && dp.NgayNhan.Month == thangChon)
                .OrderByDescending(dp => dp.NgayNhan)
                .ToList();

            return baoCao;
        }

        // ================== XUẤT EXCEL (ClosedXML) ==================

        private static byte[] TaoExcelBaoCao(DoanhThuBaoCaoViewModel baoCao)
        {
            using var wb = new XLWorkbook();

            // ---- Sheet 1: Tổng quan ----
            var tq = wb.Worksheets.Add("Tổng quan");

            tq.Cell("A1").Value = "BÁO CÁO DOANH THU KHÁCH SẠN";
            tq.Range("A1:D1").Merge();
            tq.Range("A1:D1").Style.Font.Bold = true;
            tq.Range("A1:D1").Style.Font.FontSize = 16;
            tq.Range("A1:D1").Style.Font.FontColor = XLColor.FromHtml("#172433");

            tq.Cell("A2").Value = $"Kỳ báo cáo: Tháng {baoCao.Thang}/{baoCao.Nam}";
            tq.Range("A2:D2").Merge();
            tq.Range("A2:D2").Style.Font.FontSize = 11;
            tq.Range("A2:D2").Style.Font.FontColor = XLColor.FromHtml("#59636d");

            tq.Cell("A4").Value = "Chỉ tiêu";
            tq.Cell("B4").Value = "Giá trị";
            tq.Range("A4:B4").Style.Font.Bold = true;
            tq.Range("A4:B4").Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f4f6");

            tq.Cell("A5").Value = "Doanh thu trong kỳ";
            tq.Cell("B5").Value = baoCao.DoanhThuKy;
            tq.Cell("A6").Value = "Số lượt đặt có hiệu lực";
            tq.Cell("B6").Value = baoCao.SoLuotDat;
            tq.Cell("A7").Value = "Số lượt hủy trong kỳ";
            tq.Cell("B7").Value = baoCao.SoLuotHuy;
            tq.Cell("A8").Value = "Doanh thu bình quân / lượt";
            tq.Cell("B8").Value = baoCao.DoanhThuBinhQuan;
            tq.Cell("B5").Style.NumberFormat.Format = "#,##0 \"₫\"";
            tq.Cell("B8").Style.NumberFormat.Format = "#,##0 \"₫\"";
            tq.Columns().AdjustToContents();

            // ---- Sheet 2: Doanh thu theo tháng ----
            var th = wb.Worksheets.Add("Doanh thu theo tháng");
            th.Cell(1, 1).Value = "Tháng";
            th.Cell(1, 2).Value = "Doanh thu";
            th.Cell(1, 3).Value = "Số lượt đặt";
            th.Range(1, 1, 1, 3).Style.Font.Bold = true;
            var dong = 2;
            foreach (var item in baoCao.DoanhThuTheoThang)
            {
                th.Cell(dong, 1).Value = $"Tháng {item.Thang}";
                th.Cell(dong, 2).Value = item.DoanhThu;
                th.Cell(dong, 2).Style.NumberFormat.Format = "#,##0 \"₫\"";
                th.Cell(dong, 3).Value = item.SoLuotDat;
                dong++;
            }
            if (baoCao.DoanhThuTheoThang.Count > 0)
            {
                th.Cell(dong, 1).Value = "Tổng cộng";
                th.Cell(dong, 1).Style.Font.Bold = true;
                th.Cell(dong, 2).Value = baoCao.DoanhThuTheoThang.Sum(x => x.DoanhThu);
                th.Cell(dong, 2).Style.Font.Bold = true;
                th.Cell(dong, 2).Style.NumberFormat.Format = "#,##0 \"₫\"";
                th.Cell(dong, 3).Value = baoCao.DoanhThuTheoThang.Sum(x => x.SoLuotDat);
                th.Cell(dong, 3).Style.Font.Bold = true;
            }
            th.Columns().AdjustToContents();

            // ---- Sheet 3: Doanh thu theo hạng phòng ----
            var lp = wb.Worksheets.Add("Doanh thu theo hạng phòng");
            lp.Cell(1, 1).Value = "Hạng phòng";
            lp.Cell(1, 2).Value = "Doanh thu";
            lp.Range(1, 1, 1, 2).Style.Font.Bold = true;
            dong = 2;
            foreach (var item in baoCao.DoanhThuTheoLoai)
            {
                lp.Cell(dong, 1).Value = item.TenLoai;
                lp.Cell(dong, 2).Value = item.DoanhThu;
                lp.Cell(dong, 2).Style.NumberFormat.Format = "#,##0 \"₫\"";
                dong++;
            }
            lp.Columns().AdjustToContents();

            // ---- Sheet 4: Top phòng ----
            var top = wb.Worksheets.Add("Top phòng");
            top.Cell(1, 1).Value = "Hạng";
            top.Cell(1, 2).Value = "Phòng";
            top.Cell(1, 3).Value = "Loại phòng";
            top.Cell(1, 4).Value = "Số lượt đặt";
            top.Cell(1, 5).Value = "Doanh thu";
            top.Range(1, 1, 1, 5).Style.Font.Bold = true;
            dong = 2;
            foreach (var item in baoCao.PhongDatNhieuNhat)
            {
                top.Cell(dong, 1).Value = dong - 1;
                top.Cell(dong, 2).Value = item.SoPhong;
                top.Cell(dong, 3).Value = item.TenLoai;
                top.Cell(dong, 4).Value = item.SoLuotDat;
                top.Cell(dong, 5).Value = item.DoanhThu;
                top.Cell(dong, 5).Style.NumberFormat.Format = "#,##0 \"₫\"";
                dong++;
            }
            top.Columns().AdjustToContents();

            // ---- Sheet 5: Chi tiết đơn trong kỳ ----
            var ct = wb.Worksheets.Add("Chi tiết đơn trong kỳ");
            string[] tieuDe = { "Mã đơn", "Khách hàng", "Số điện thoại", "Nhận phòng", "Trả phòng", "Số người", "Trạng thái", "Tổng tiền" };
            for (var c = 0; c < tieuDe.Length; c++)
            {
                ct.Cell(1, c + 1).Value = tieuDe[c];
            }
            ct.Range(1, 1, 1, tieuDe.Length).Style.Font.Bold = true;
            dong = 2;
            foreach (var dp in baoCao.ChiTietKy)
            {
                ct.Cell(dong, 1).Value = dp.MaDatPhong;
                ct.Cell(dong, 2).Value = dp.KhachHang?.HoTen ?? "—";
                ct.Cell(dong, 3).Value = dp.KhachHang?.SoDienThoai ?? "—";
                ct.Cell(dong, 4).Value = dp.NgayNhan.ToString("dd/MM/yyyy");
                ct.Cell(dong, 5).Value = dp.NgayTraDuKien.ToString("dd/MM/yyyy");
                ct.Cell(dong, 6).Value = dp.SoNguoi;
                ct.Cell(dong, 7).Value = TrangThaiHelper.HienThi(dp.TrangThai);
                ct.Cell(dong, 8).Value = dp.TongTien;
                ct.Cell(dong, 8).Style.NumberFormat.Format = "#,##0 \"₫\"";
                dong++;
            }
            ct.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ================== XUẤT PDF (QuestPDF) ==================

        private static byte[] TaoPdfBaoCao(DoanhThuBaoCaoViewModel baoCao)
        {
            var taiLieu = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9.5f).FontColor("#243340"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("BÁO CÁO DOANH THU KHÁCH SẠN").FontSize(17).Bold().FontColor("#172433");
                        col.Item().PaddingTop(2).Text($"Kỳ báo cáo: Tháng {baoCao.Thang}/{baoCao.Nam}")
                            .FontSize(10).FontColor("#59636d");
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#e6eaed");
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(14);

                        // Bảng tóm tắt đầu trang.
                        col.Item().Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Element(SummaryBox).Column(c =>
                            {
                                c.Item().Text("Doanh thu trong kỳ").FontSize(8.5f).FontColor("#737d87");
                                c.Item().Text($"{baoCao.DoanhThuKy:N0} ₫").Bold().FontSize(13).FontColor("#b28c49");
                            });
                            row.RelativeItem().Element(SummaryBox).Column(c =>
                            {
                                c.Item().Text("Số lượt đặt (hiệu lực)").FontSize(8.5f).FontColor("#737d87");
                                c.Item().Text(baoCao.SoLuotDat.ToString()).Bold().FontSize(13).FontColor("#1f3a5f");
                            });
                            row.RelativeItem().Element(SummaryBox).Column(c =>
                            {
                                c.Item().Text("Số lượt hủy").FontSize(8.5f).FontColor("#737d87");
                                c.Item().Text(baoCao.SoLuotHuy.ToString()).Bold().FontSize(13).FontColor("#c0392b");
                            });
                        });

                        // Doanh thu theo tháng.
                        col.Item().Text("Doanh thu theo tháng").Bold().FontSize(11).FontColor("#172433");
                        if (baoCao.DoanhThuTheoThang.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Tháng");
                                    header.Cell().Element(HeaderCell).Text("Doanh thu");
                                    header.Cell().Element(HeaderCell).Text("Số lượt đặt");
                                });
                                foreach (var item in baoCao.DoanhThuTheoThang)
                                {
                                    table.Cell().Element(BodyCell).Text($"Tháng {item.Thang}");
                                    table.Cell().Element(BodyCell).Text($"{item.DoanhThu:N0} ₫");
                                    table.Cell().Element(BodyCell).Text(item.SoLuotDat.ToString());
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Chưa có dữ liệu trong kỳ.").Italic().FontColor("#9aa2aa");
                        }

                        // Doanh thu theo hạng phòng.
                        col.Item().Text("Doanh thu theo hạng phòng").Bold().FontSize(11).FontColor("#172433");
                        if (baoCao.DoanhThuTheoLoai.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Hạng phòng");
                                    header.Cell().Element(HeaderCell).Text("Doanh thu");
                                });
                                foreach (var item in baoCao.DoanhThuTheoLoai)
                                {
                                    table.Cell().Element(BodyCell).Text(item.TenLoai);
                                    table.Cell().Element(BodyCell).Text($"{item.DoanhThu:N0} ₫");
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Chưa có dữ liệu trong kỳ.").Italic().FontColor("#9aa2aa");
                        }

                        // Top phòng.
                        col.Item().Text("Phòng được đặt nhiều nhất trong kỳ").Bold().FontSize(11).FontColor("#172433");
                        if (baoCao.PhongDatNhieuNhat.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Hạng");
                                    header.Cell().Element(HeaderCell).Text("Phòng");
                                    header.Cell().Element(HeaderCell).Text("Loại phòng");
                                    header.Cell().Element(HeaderCell).Text("Lượt đặt");
                                    header.Cell().Element(HeaderCell).Text("Doanh thu");
                                });
                                var hang = 1;
                                foreach (var item in baoCao.PhongDatNhieuNhat)
                                {
                                    table.Cell().Element(BodyCell).Text(hang.ToString());
                                    table.Cell().Element(BodyCell).Text(item.SoPhong);
                                    table.Cell().Element(BodyCell).Text(item.TenLoai);
                                    table.Cell().Element(BodyCell).Text(item.SoLuotDat.ToString());
                                    table.Cell().Element(BodyCell).Text($"{item.DoanhThu:N0} ₫");
                                    hang++;
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Chưa có lượt đặt nào trong kỳ.").Italic().FontColor("#9aa2aa");
                        }

                        // Chi tiết đơn trong kỳ.
                        col.Item().Text("Chi tiết các đơn đặt phòng trong kỳ").Bold().FontSize(11).FontColor("#172433");
                        if (baoCao.ChiTietKy.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(45);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(1.6f);
                                    columns.RelativeColumn(1.6f);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Mã");
                                    header.Cell().Element(HeaderCell).Text("Khách hàng");
                                    header.Cell().Element(HeaderCell).Text("Điện thoại");
                                    header.Cell().Element(HeaderCell).Text("Nhận phòng");
                                    header.Cell().Element(HeaderCell).Text("Trả phòng");
                                    header.Cell().Element(HeaderCell).Text("Trạng thái");
                                    header.Cell().Element(HeaderCell).Text("Tổng tiền");
                                });
                                foreach (var dp in baoCao.ChiTietKy)
                                {
                                    table.Cell().Element(BodyCell).Text($"#{dp.MaDatPhong}");
                                    table.Cell().Element(BodyCell).Text(dp.KhachHang?.HoTen ?? "—");
                                    table.Cell().Element(BodyCell).Text(dp.KhachHang?.SoDienThoai ?? "—");
                                    table.Cell().Element(BodyCell).Text(dp.NgayNhan.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(BodyCell).Text(dp.NgayTraDuKien.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(BodyCell).Text(TrangThaiHelper.HienThi(dp.TrangThai));
                                    table.Cell().Element(BodyCell).Text($"{dp.TongTien:N0} ₫");
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Chưa có đơn đặt phòng nào trong kỳ.").Italic().FontColor("#9aa2aa");
                        }
                    });

                    page.Footer().AlignCenter().DefaultTextStyle(t => t.FontSize(8).FontColor("#9aa2aa")).Text(text =>
                    {
                        text.Span("Trang ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            return taiLieu.GeneratePdf();
        }

        private static IContainer SummaryBox(IContainer container)
        {
            return container.Border(1).BorderColor("#e6eaed").Background("#f7f9fa").Padding(8);
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background("#1f3a5f")
                .PaddingVertical(4)
                .PaddingHorizontal(6)
                .DefaultTextStyle(t => t.FontColor("#ffffff").Bold());
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor("#e6eaed")
                .PaddingVertical(4)
                .PaddingHorizontal(6);
        }
    }
}
