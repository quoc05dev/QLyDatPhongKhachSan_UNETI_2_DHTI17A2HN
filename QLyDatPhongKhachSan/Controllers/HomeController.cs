using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers;

// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung thực hiện: Trang chủ công khai theo bản thiết kế DesignProjectWithSuggestions
// (hero, bảng đặt phòng, hạng phòng nổi bật lấy dữ liệu thật từ Phong/LoaiPhong).
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    // Hình minh họa mẫu dự phòng theo loại phòng khi cột LoaiPhong.HinhAnh chưa có dữ liệu
    // (lấy từ bản thiết kế DesignProjectWithSuggestions + kho ảnh Unsplash).
    private static readonly Dictionary<string, string> AnhMauTheoLoai =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Phòng Tiêu Chuẩn"] = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1100&q=86",
            ["Phòng Nâng Cao"] = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1100&q=86",
            ["Phòng Cao Cấp"] = "https://images.unsplash.com/photo-1668277155881-9d3bccb1683a?auto=format&fit=crop&w=1100&q=86",
            ["Phòng Suite"] = "https://images.unsplash.com/photo-1556426983-5705abbfcad6?auto=format&fit=crop&w=1100&q=86",
            ["Phòng Hai Phòng"] = "https://images.unsplash.com/photo-1776876648949-63ccabf63b10?auto=format&fit=crop&w=1100&q=86",
        };

    public IActionResult Index(string? loai)
    {
        var loaiPhongs = _context.LoaiPhongs
            .AsNoTracking()
            .Where(lp => lp.TrangThai)
            .OrderBy(lp => lp.MaLoaiPhong)
            .ToList();

        var phongs = _context.Phongs
            .AsNoTracking()
            .Include(p => p.LoaiPhong)
            .Where(p => p.LoaiPhong != null && p.LoaiPhong.TrangThai && p.TrangThai != "BaoTri")
            .ToList();

        // Mỗi loại phòng lấy một phòng đại diện để tạo lưới "hạng phòng nổi bật".
        var labels = new[] { "Được yêu thích", "Bữa sáng miễn phí", "Trải nghiệm độc bản" };
        var cards = new List<HomePhongCard>();
        var index = 0;

        foreach (var group in phongs.GroupBy(p => p.MaLoaiPhong))
        {
            var daiDien = group.First();
            var loaiPhong = daiDien.LoaiPhong!;

            var tienNghi = string.IsNullOrWhiteSpace(daiDien.TienNghi) ? null : daiDien.TienNghi;
            var huong = string.IsNullOrWhiteSpace(daiDien.HuongPhong) ? "View đẹp" : daiDien.HuongPhong;

            cards.Add(new HomePhongCard
            {
                MaPhong = daiDien.MaPhong,
                SoPhong = daiDien.SoPhong,
                TenLoai = loaiPhong.TenLoai,
                Meta = $"Phòng tầng {daiDien.Tang} · Sức chứa {loaiPhong.SoNguoiToiDa} khách · {huong}" +
                       (tienNghi == null ? string.Empty : $" · {tienNghi}"),
                MoTa = string.IsNullOrWhiteSpace(loaiPhong.MoTa)
                    ? "Không gian nghỉ dưỡng tinh tuyển với đầy đủ tiện nghi, được hoàn thiện để mang đến sự thư thái trọn vẹn."
                    : loaiPhong.MoTa!,
                DonGia = daiDien.DonGia,
                HinhAnh = string.IsNullOrWhiteSpace(loaiPhong.HinhAnh)
                    ? (AnhMauTheoLoai.TryGetValue(loaiPhong.TenLoai, out var anhMau) ? anhMau : string.Empty)
                    : loaiPhong.HinhAnh!,
                Label = labels[index % labels.Length]
            });

            index++;
        }

        var model = new HomeViewModel
        {
            Phongs = cards,
            LoaiPhongs = loaiPhongs.Select(lp => lp.TenLoai).ToList(),
            LoaiPhongFilter = loai,
            IsLoggedIn = HttpContext.Session.IsLoggedIn(),
            VaiTro = HttpContext.Session.GetVaiTro(),
            HoTen = HttpContext.Session.GetHoTen()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}