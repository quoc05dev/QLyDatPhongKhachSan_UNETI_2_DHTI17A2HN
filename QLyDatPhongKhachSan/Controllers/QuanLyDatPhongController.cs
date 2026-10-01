// Ho va ten: Nguyen Hai Nam
// Ma sinh vien: 23103100118
// Noi dung: Quan ly tiep nhan dat phong, kiem tra phong trung LINQ, Check-in va Check-out

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class QuanLyDatPhongController : Controller
    {
        private readonly AppDbContext _context;

        public QuanLyDatPhongController(AppDbContext context)
        {
            _context = context;
        }
        private bool KiemTraQuyenNhanVien()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin" || vaiTro == "NhanVien";
        }

        // 1. DANH SÁCH TIẾP NHẬN: TÌM KIẾM, LỌC, PHÂN TRANG
        public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int? maLoaiPhong, 
                                               DateTime? tuNgay, DateTime? denNgay, string? sapXep, int trang = 1)
        {
            //if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            int pageSize = 10;
            var query = _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim().ToLower();
                query = query.Where(d => d.KhachHang.HoTen.ToLower().Contains(tuKhoa) || 
                                         d.KhachHang.SoDienThoai.Contains(tuKhoa) ||
                                         d.MaDatPhong.ToString().Contains(tuKhoa));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            if (maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(d => d.ChiTietDatPhongs.Any(ct => ct.Phong.MaLoaiPhong == maLoaiPhong.Value));
            }

            if (tuNgay.HasValue) query = query.Where(d => d.NgayNhan >= tuNgay.Value);
            if (denNgay.HasValue) query = query.Where(d => d.NgayNhan <= denNgay.Value);

            query = sapXep switch
            {
                "ngaynhan_asc" => query.OrderBy(d => d.NgayNhan),
                "ngaynhan_desc" => query.OrderByDescending(d => d.NgayNhan),
                "tenkhach_asc" => query.OrderBy(d => d.KhachHang.HoTen),
                _ => query.OrderByDescending(d => d.NgayDat)
            };

            int totalRecords = await query.CountAsync();
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
                TongSoTrang = (int)Math.Ceiling((double)totalRecords / pageSize),
                TongBanGhi = totalRecords
            };

            return View(viewModel);
        }

        // 2. CHI TIẾT ĐẶT PHÒNG
        public async Task<IActionResult> ChiTiet(int id)
        {
            if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            var datPhong = await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == id);

            if (datPhong == null) return NotFound();

            return View(datPhong);
        }

        // 3. TIẾP NHẬN / DUYỆT ĐƠN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDatPhong(int maDatPhong)
        {
            if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "ChoXuLy")
            {
                TempData["Error"] = "Chỉ có thể duyệt đơn ở trạng thái 'Chờ xử lý'!";
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
                if (p.TrangThai == "BaoTri")
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đang bảo trì, không thể chấp nhận!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                if (datPhong.SoNguoi > p.LoaiPhong.SoNguoiToiDa)
                {
                    TempData["Error"] = $"Số người ({datPhong.SoNguoi}) vượt quá sức chứa phòng {p.SoPhong} ({p.LoaiPhong.SoNguoiToiDa} người)!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }

                bool biTrungLich = await _context.ChiTietDatPhongs
                    .Include(c => c.DatPhong)
                    .AnyAsync(c => c.MaPhong == ct.MaPhong 
                                   && c.MaDatPhong != maDatPhong
                                   && c.DatPhong.TrangThai != "DaHuy" 
                                   && c.DatPhong.TrangThai != "HoanThanh"
                                   && datPhong.NgayNhan < c.DatPhong.NgayTraDuKien 
                                   && datPhong.NgayTraDuKien > c.DatPhong.NgayNhan);

                if (biTrungLich)
                {
                    TempData["Error"] = $"Phòng {p.SoPhong} đã có khách đặt trùng thời gian này!";
                    return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
                }
            }

            datPhong.TrangThai = "DaXacNhan";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt và xác nhận đặt phòng thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 4. CHECK-IN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int maDatPhong)
        {
            if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "DaXacNhan")
            {
                TempData["Error"] = "Chỉ đơn 'Đã xác nhận' mới được làm thủ tục nhận phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            datPhong.TrangThai = "DangXuLy";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.Phong.TrangThai = "DangCoKhach";
                ct.NgayNhan = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thủ tục nhận phòng hoàn tất!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 5. CHECK-OUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int maDatPhong)
        {
            if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            var datPhong = await _context.DatPhongs
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai != "DangXuLy")
            {
                TempData["Error"] = "Chỉ đơn đang ở mới được làm thủ tục trả phòng!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            datPhong.TrangThai = "HoanThanh";

            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.Phong.TrangThai = "Trong";
                ct.NgayTra = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thủ tục trả phòng hoàn tất! Phòng đã được giải phóng.";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }

        // 6. HỦY ĐẶT PHÒNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDatPhong(int maDatPhong, string lyDoHuy)
        {
            if (!KiemTraQuyenNhanVien()) return RedirectToAction("Login", "DangNhap");

            var datPhong = await _context.DatPhongs.FindAsync(maDatPhong);
            if (datPhong == null) return NotFound();

            if (datPhong.TrangThai == "DangXuLy" || datPhong.TrangThai == "HoanThanh")
            {
                TempData["Error"] = "Đơn đã nhận phòng hoặc đã hoàn thành, không được phép hủy!";
                return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
            }

            datPhong.TrangThai = "DaHuy";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"Ly do huy: {lyDoHuy}"
                : $"{datPhong.GhiChu} | Ly do huy: {lyDoHuy}";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy đơn đặt phòng thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = maDatPhong });
        }
    }
}