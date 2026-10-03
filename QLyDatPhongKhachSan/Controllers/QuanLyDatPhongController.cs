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
                query = query.Where(d => d.KhachHang!.HoTen.ToLower().Contains(tuKhoa) || 
                                         d.KhachHang!.SoDienThoai.Contains(tuKhoa) ||
                                         d.MaDatPhong.ToString().Contains(tuKhoa));
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

            query = sapXep switch
            {
                "ngaynhan_asc" => query.OrderBy(d => d.NgayNhan),
                "ngaynhan_desc" => query.OrderByDescending(d => d.NgayNhan),
                "tenkhach_asc" => query.OrderBy(d => d.KhachHang!.HoTen),
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
    }
}