// Ho va ten: Nguyen Hai Nam
// Ma sinh vien: 22103100118
// Noi dung: Quan ly tiep nhan dat phong, kiem tra phong trung LINQ, Check-in va Check-out

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;
using QLyDatPhongKhachSan.Filters;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin", "NhanVien")]
    public class QuanLyDatPhongController : Controller
    {
        private readonly AppDbContext _context;

        public QuanLyDatPhongController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH TIẾP NHẬN: TÌM KIẾM, LỌC, PHÂN TRANG
        public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int? maLoaiPhong, 
                                               DateTime? tuNgay, DateTime? denNgay, string? sapXep, int trang = 1)
        {
            // Kiểm tra trang hợp lệ — tránh Skip âm gây lỗi SQL 500
            if (trang < 1) trang = 1;

            int pageSize = 10;
            var query = _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim().ToLower();
                // 8.1: Tim kiem theo nguoi dung (ten, sdt), doi tuong chinh (so phong) va ma don
                query = query.Where(d => d.KhachHang!.HoTen.ToLower().Contains(tuKhoa) || 
                                         d.KhachHang!.SoDienThoai.Contains(tuKhoa) ||
                                         d.MaDatPhong.ToString().Contains(tuKhoa) ||
                                         d.ChiTietDatPhongs.Any(ct => ct.Phong!.SoPhong.ToLower().Contains(tuKhoa)));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            if (maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(d => d.ChiTietDatPhongs.Any(ct => ct.Phong!.MaLoaiPhong == maLoaiPhong.Value));
            }

            // Lọc khoảng ngày đúng biên — dùng .Date và .AddDays(1) để bao trọn ngày cuối
            if (tuNgay.HasValue) query = query.Where(d => d.NgayNhan >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(d => d.NgayNhan < denNgay.Value.Date.AddDays(1));

            // 8.1: Sắp xếp theo ngày hoặc tên
            query = sapXep switch
            {
                "ngaynhan_asc" => query.OrderBy(d => d.NgayNhan),
                "ngaynhan_desc" => query.OrderByDescending(d => d.NgayNhan),
                "tenkhach_asc" => query.OrderBy(d => d.KhachHang!.HoTen),
                "tenkhach_desc" => query.OrderByDescending(d => d.KhachHang!.HoTen),
                "ngaydat_asc" => query.OrderBy(d => d.NgayDat),
                _ => query.OrderByDescending(d => d.NgayDat)
            };

            int totalRecords = await query.CountAsync();

            // Đảm bảo trang không vượt quá tổng số trang
            int tongSoTrang = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (tongSoTrang > 0 && trang > tongSoTrang) trang = tongSoTrang;

            var danhSach = await query.Skip((trang - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.LoaiPhongs = new SelectList(await _context.LoaiPhongs.ToListAsync(), "MaLoaiPhong", "TenLoai", maLoaiPhong);

            var viewModel = new TiepNhanDatPhongViewModel
            {
                DanhSachDatPhong = danhSach,
                TuKhoa = tuKhoa,
                TrangThai = trangThai,
                MaLoaiPhong = maLoaiPhong,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                SapXep = sapXep,
                TrangHienTai = trang,
                TongSoTrang = tongSoTrang,
                TongBanGhi = totalRecords
            };

            return View(viewModel);
        }

        // 2. CHI TIẾT ĐẶT PHÒNG
        public async Task<IActionResult> ChiTiet(int id)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (datPhong == null) return NotFound();

            // Yêu cầu 8.5: Sử dụng LINQ kiểm soát số lượng / khả năng đáp ứng của phòng
            var thongTinKhaNangDapUng = new List<string>();
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                if (ct.Phong?.LoaiPhong != null)
                {
                    int maLoai = ct.Phong.MaLoaiPhong;
                    int tongPhongLoai = await _context.Phongs
                        .CountAsync(p => p.MaLoaiPhong == maLoai && p.TrangThai != "BaoTri");

                    int phongDaDat = await _context.ChiTietDatPhongs
                        .Where(c => c.Phong!.MaLoaiPhong == maLoai
                                    && c.MaDatPhong != id
                                    && c.DatPhong!.TrangThai != "DaHuy"
                                    && c.DatPhong!.TrangThai != "HoanThanh"
                                    && datPhong.NgayNhan < c.DatPhong!.NgayTraDuKien
                                    && datPhong.NgayTraDuKien > c.DatPhong!.NgayNhan)
                        .Select(c => c.MaPhong)
                        .Distinct()
                        .CountAsync();

                    int phongConTrong = tongPhongLoai - phongDaDat;
                    thongTinKhaNangDapUng.Add($"Phòng {ct.Phong.SoPhong} ({ct.Phong.LoaiPhong.TenLoai}): Còn {phongConTrong}/{tongPhongLoai} phòng cùng loại trống trong khoảng thời gian này.");
                }
            }
            ViewBag.ThongTinKhaNangDapUng = thongTinKhaNangDapUng;

            // Nghiệp vụ Lễ tân: Lấy danh sách các phòng trống cùng loại có thể đổi (nếu đơn chưa nhận phòng)
            var phongDoiKhaDung = new Dictionary<int, List<Phong>>();
            if (datPhong.TrangThai == "ChoXuLy" || datPhong.TrangThai == "DangXuLy" || datPhong.TrangThai == "DaXacNhan")
            {
                var phongDaChonTrongDon = datPhong.ChiTietDatPhongs.Select(c => c.MaPhong).ToList();
                foreach (var ct in datPhong.ChiTietDatPhongs)
                {
                    if (ct.Phong != null)
                    {
                        int maLoai = ct.Phong.MaLoaiPhong;

                        // Tìm các phòng đã bị đặt trùng lịch trong khoảng thời gian này
                        var phongBanIds = await _context.ChiTietDatPhongs
                            .Where(c => c.Phong!.MaLoaiPhong == maLoai
                                        && c.MaDatPhong != id
                                        && c.DatPhong!.TrangThai != "DaHuy"
                                        && c.DatPhong!.TrangThai != "HoanThanh"
                                        && datPhong.NgayNhan < c.DatPhong!.NgayTraDuKien
                                        && datPhong.NgayTraDuKien > c.DatPhong!.NgayNhan)
                            .Select(c => c.MaPhong)
                            .Distinct()
                            .ToListAsync();

                        // Lấy các phòng cùng loại, không bảo trì, không bị trùng lịch và khác các phòng đã chọn trong đơn
                        var phongTrongCungLoai = await _context.Phongs
                            .Where(p => p.MaLoaiPhong == maLoai
                                        && p.TrangThai != "BaoTri"
                                        && !phongDaChonTrongDon.Contains(p.MaPhong)
                                        && !phongBanIds.Contains(p.MaPhong))
                            .OrderBy(p => p.SoPhong)
                            .ToListAsync();

                        phongDoiKhaDung[ct.MaChiTiet] = phongTrongCungLoai;
                    }
                }
            }
            ViewBag.PhongDoiKhaDung = phongDoiKhaDung;

            return View(datPhong);
        }

        // 3. TIẾP NHẬN ĐƠN: ChoXuLy → DangXuLy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TiepNhanDon(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "ChoXuLy")
            {
                TempData["Error"] = "Chỉ có thể tiếp nhận đơn ở trạng thái 'Chờ xử lý'!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            datPhong.TrangThai = "DangXuLy";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã tiếp nhận đơn đặt phòng thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 4. DUYỆT / XÁC NHẬN ĐƠN: DangXuLy → DaXacNhan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanDatPhong(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "DangXuLy")
            {
                TempData["Error"] = "Chỉ có thể xác nhận đơn ở trạng thái 'Đang xử lý'!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            if (datPhong.NgayTraDuKien <= datPhong.NgayNhan)
            {
                TempData["Error"] = "Ngày trả dự kiến phải sau ngày nhận phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                var p = ct.Phong;
                if (p == null)
                {
                    TempData["Error"] = $"Dữ liệu phòng của chi tiết #{ct.MaChiTiet} bị thiếu!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (p.TrangThai == "BaoTri")
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đang bảo trì, không thể chấp nhận!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (p.LoaiPhong == null)
                {
                    TempData["Error"] = $"Dữ liệu loại phòng của phòng {p.SoPhong} bị thiếu!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (datPhong.SoNguoi > p.LoaiPhong.SoNguoiToiDa)
                {
                    TempData["Error"] = $"Số người ({datPhong.SoNguoi}) vượt quá sức chứa phòng {p.SoPhong} ({p.LoaiPhong.SoNguoiToiDa} người)!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                // Cảnh báo giới hạn sức chứa (8.5)
                if (datPhong.SoNguoi == p.LoaiPhong.SoNguoiToiDa)
                {
                    TempData["Warning"] = $"Lưu ý: Phòng {p.SoPhong} đã đạt sức chứa tối đa ({p.LoaiPhong.SoNguoiToiDa} người).";
                }

                bool biTrungLich = await _context.ChiTietDatPhongs
                    .Include(c => c.DatPhong)
                    .AnyAsync(c => c.MaPhong == ct.MaPhong 
                                   && c.MaDatPhong != maDatPhong
                                   && c.DatPhong!.TrangThai != "DaHuy" 
                                   && c.DatPhong!.TrangThai != "HoanThanh"
                                   && datPhong.NgayNhan < c.DatPhong!.NgayTraDuKien 
                                   && datPhong.NgayTraDuKien > c.DatPhong!.NgayNhan);

                if (biTrungLich)
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đã có khách đặt trùng thời gian này!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                // Yêu cầu 8.5: Kiểm soát khả năng đáp ứng bằng LINQ đếm số lượng
                int tongPhongLoai = await _context.Phongs
                    .CountAsync(ph => ph.MaLoaiPhong == p.MaLoaiPhong && ph.TrangThai != "BaoTri");

                int phongDaBan = await _context.ChiTietDatPhongs
                    .Where(c => c.Phong!.MaLoaiPhong == p.MaLoaiPhong
                                && c.MaDatPhong != maDatPhong
                                && c.DatPhong!.TrangThai != "DaHuy"
                                && c.DatPhong!.TrangThai != "HoanThanh"
                                && datPhong.NgayNhan < c.DatPhong!.NgayTraDuKien
                                && datPhong.NgayTraDuKien > c.DatPhong!.NgayNhan)
                    .Select(c => c.MaPhong)
                    .Distinct()
                    .CountAsync();

                int conLai = tongPhongLoai - phongDaBan - 1; // sau khi duyệt đơn này
                if (conLai <= 0)
                {
                    TempData["Warning"] = $"Lưu ý (Yêu cầu 8.5): Loại phòng '{p.LoaiPhong.TenLoai}' sẽ hết phòng trống sau khi xác nhận đơn này!";
                }
                else if (conLai <= 2)
                {
                    TempData["Warning"] = $"Lưu ý (Yêu cầu 8.5): Loại phòng '{p.LoaiPhong.TenLoai}' chỉ còn {conLai} phòng trống khả dụng trong khoảng thời gian này.";
                }
            }

            // Chuyển phòng sang trạng thái DaDat — giữ chỗ tránh TOCTOU
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.Phong!.TrangThai = "DaDat";
            }

            datPhong.TrangThai = "DaXacNhan";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt và xác nhận đặt phòng thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 5. CHECK-IN: DaXacNhan → DangSuDung
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "DaXacNhan")
            {
                TempData["Error"] = "Chỉ đơn 'Đã xác nhận' mới được làm thủ tục nhận phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            // Kiểm tra lại điều kiện phòng trước khi check-in (tránh thay đổi giữa lúc duyệt và check-in)
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                var p = ct.Phong;
                if (p == null)
                {
                    TempData["Error"] = $"Dữ liệu phòng của chi tiết #{ct.MaChiTiet} bị thiếu!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (p.TrangThai == "BaoTri")
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đang bảo trì, không thể nhận phòng!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (p.TrangThai == "DangSuDung")
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đang có khách sử dụng!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (p.LoaiPhong != null && datPhong.SoNguoi > p.LoaiPhong.SoNguoiToiDa)
                {
                    TempData["Error"] = $"Số người ({datPhong.SoNguoi}) vượt quá sức chứa phòng {p.SoPhong} ({p.LoaiPhong.SoNguoiToiDa} người)!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }
            }

            datPhong.TrangThai = "DangSuDung";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.Phong!.TrangThai = "DangSuDung";
                ct.NgayNhan = DateTime.Now;
                ct.TrangThai = "DaNhan";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thủ tục nhận phòng hoàn tất!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 6. CHECK-OUT: DangSuDung → HoanThanh (tính tiền + cập nhật trạng thái)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "DangSuDung")
            {
                TempData["Error"] = "Chỉ đơn đang sử dụng mới được làm thủ tục trả phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            datPhong.TrangThai = "HoanThanh";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                if (ct.Phong == null) continue;

                // Chỉ trả phòng về "Trong" nếu phòng đang "DangSuDung" — không ghi đè BaoTri
                if (ct.Phong.TrangThai == "DangSuDung")
                {
                    ct.Phong.TrangThai = "Trong";
                }

                ct.NgayTra = DateTime.Now;
                ct.TrangThai = "DaTra";
                // Tính tiền cho từng chi tiết
                ChiTietDatPhongController.ApDungCongThucTinhTien(ct);
            }

            // Cập nhật tổng tiền cho đơn đặt phòng
            datPhong.TongTien = ChiTietDatPhongController.TinhTongTien(datPhong.ChiTietDatPhongs);

            // Ghi người thực hiện
            var nguoiThucHien = HttpContext.Session.GetString("TenDangNhap") ?? "Hệ thống";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"Trả phòng bởi: {nguoiThucHien} lúc {DateTime.Now:dd/MM/yyyy HH:mm}"
                : $"{datPhong.GhiChu} | Trả phòng bởi: {nguoiThucHien} lúc {DateTime.Now:dd/MM/yyyy HH:mm}";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Thủ tục trả phòng hoàn tất! Tổng tiền: {datPhong.TongTien:N0} VNĐ";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 7. HỦY ĐẶT PHÒNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDatPhong(int maDatPhong, string lyDoHuy)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai == "DangSuDung" || datPhong.TrangThai == "HoanThanh")
            {
                TempData["Error"] = "Đơn đã nhận phòng hoặc đã hoàn thành, không được phép hủy!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            // Ghi người thực hiện từ Session
            var nguoiThucHien = HttpContext.Session.GetString("TenDangNhap") ?? "Hệ thống";

            datPhong.TrangThai = "DaHuy";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"Hủy bởi {nguoiThucHien}: {lyDoHuy}"
                : $"{datPhong.GhiChu} | Hủy bởi {nguoiThucHien}: {lyDoHuy}";

            // Cập nhật trạng thái chi tiết + giải phóng phòng đã đặt
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.TrangThai = "DaHuy";
                if (ct.Phong != null && (ct.Phong.TrangThai == "DaDat" || ct.Phong.TrangThai == "DangXuLy"))
                {
                    ct.Phong.TrangThai = "Trong";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy đơn đặt phòng thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 8. TỪ CHỐI ĐƠN ĐẶT PHÒNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiDatPhong(int maDatPhong, string lyDoTuChoi)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "ChoXuLy" && datPhong.TrangThai != "DangXuLy")
            {
                TempData["Error"] = "Chỉ có thể từ chối đơn ở trạng thái 'Chờ xử lý' hoặc 'Đang xử lý'!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            var nguoiThucHien = HttpContext.Session.GetString("TenDangNhap") ?? "Hệ thống";

            datPhong.TrangThai = "DaHuy";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"Từ chối bởi {nguoiThucHien}: {lyDoTuChoi}"
                : $"{datPhong.GhiChu} | Từ chối bởi {nguoiThucHien}: {lyDoTuChoi}";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.TrangThai = "DaHuy";
                if (ct.Phong != null && (ct.Phong.TrangThai == "DaDat" || ct.Phong.TrangThai == "DangXuLy"))
                {
                    ct.Phong.TrangThai = "Trong";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối đơn đặt phòng!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 9. ĐỔI PHÒNG CÙNG LOẠI: Hỗ trợ lễ tân đổi phòng trống cùng loại cho khách trước khi check-in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiPhong(int maDatPhong, int maChiTiet, int maPhongMoi)
        {
            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "ChoXuLy" && datPhong.TrangThai != "DangXuLy" && datPhong.TrangThai != "DaXacNhan")
            {
                TempData["Error"] = "Chỉ có thể đổi phòng khi đơn chưa làm thủ tục nhận phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            var chiTiet = datPhong.ChiTietDatPhongs.FirstOrDefault(c => c.MaChiTiet == maChiTiet);
            if (chiTiet == null || chiTiet.Phong == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin chi tiết phòng cần đổi!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            var phongCu = chiTiet.Phong;
            var phongMoi = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhongMoi);

            if (phongMoi == null)
            {
                TempData["Error"] = "Phòng mới không tồn tại trên hệ thống!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            if (phongMoi.MaLoaiPhong != phongCu.MaLoaiPhong)
            {
                TempData["Error"] = "Chỉ được phép đổi sang phòng cùng Loại phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            if (phongMoi.TrangThai == "BaoTri")
            {
                TempData["Error"] = $"Phòng {phongMoi.SoPhong} đang trong trạng thái bảo trì, không thể đổi!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            // Kiểm tra phòng mới có bị trùng lịch trong khoảng ngày này không
            bool biTrungLich = await _context.ChiTietDatPhongs
                .Include(c => c.DatPhong)
                .AnyAsync(c => c.MaPhong == maPhongMoi 
                               && c.MaDatPhong != maDatPhong
                               && c.DatPhong!.TrangThai != "DaHuy" 
                               && c.DatPhong!.TrangThai != "HoanThanh"
                               && datPhong.NgayNhan < c.DatPhong!.NgayTraDuKien 
                               && datPhong.NgayTraDuKien > c.DatPhong!.NgayNhan);

            if (biTrungLich)
            {
                TempData["Error"] = $"Phòng {phongMoi.SoPhong} đã có khách khác đặt trong thời gian này!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            // Kiểm tra xem phòng mới có bị trùng với phòng nào khác trong cùng đơn đặt này không
            if (datPhong.ChiTietDatPhongs.Any(c => c.MaChiTiet != maChiTiet && c.MaPhong == maPhongMoi))
            {
                TempData["Error"] = $"Phòng {phongMoi.SoPhong} đã được gán cho phòng khác trong chính đơn đặt này!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            // Giải phóng phòng cũ nếu đơn đã xác nhận
            if (datPhong.TrangThai == "DaXacNhan")
            {
                phongCu.TrangThai = "Trong";
                phongMoi.TrangThai = "DaDat";
            }

            string soPhongCu = phongCu.SoPhong;
            chiTiet.MaPhong = maPhongMoi;
            chiTiet.Phong = phongMoi;
            chiTiet.DonGia = phongMoi.DonGia;

            // Ghi nhật ký đổi phòng vào GhiChu
            var nguoiThucHien = HttpContext.Session.GetString("TenDangNhap") ?? "Lễ tân";
            string logDoi = $"Đổi từ phòng {soPhongCu} sang phòng {phongMoi.SoPhong} bởi {nguoiThucHien} lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu) ? logDoi : $"{datPhong.GhiChu} | {logDoi}";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã đổi thành công từ phòng {soPhongCu} sang phòng {phongMoi.SoPhong}!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }
    }
}