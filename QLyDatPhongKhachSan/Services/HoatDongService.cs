using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Log hành vi người dùng (đăng nhập, xem phòng, đặt phòng, đánh giá)
    /// và quản lý chuông thông báo của trang quản trị (đơn đặt mới, đánh giá mới).
    /// </summary>
    public interface IHoatDongService
    {
        Task GhiAsync(string loaiHoatDong, string moTa, int? maTaiKhoan = null,
                      string? tenNguoiDung = null, int? maLienQuan = null, bool laThongBao = false);
        Task<List<HoatDong>> LayThongBaoAsync(int soLuong);
        Task<int> DemChuaDocAsync();
        Task DanhDauDaDocAsync();
    }

    public class HoatDongService : IHoatDongService
    {
        private readonly AppDbContext _db;

        public HoatDongService(AppDbContext db)
        {
            _db = db;
        }

        public async Task GhiAsync(string loaiHoatDong, string moTa, int? maTaiKhoan = null,
                                   string? tenNguoiDung = null, int? maLienQuan = null, bool laThongBao = false)
        {
            _db.HoatDongs.Add(new HoatDong
            {
                LoaiHoatDong = loaiHoatDong,
                MoTa = moTa.Length > 500 ? moTa[..500] : moTa,
                MaTaiKhoan = maTaiKhoan,
                TenNguoiDung = tenNguoiDung,
                MaLienQuan = maLienQuan,
                LaThongBao = laThongBao,
                DaDoc = false
            });
            await _db.SaveChangesAsync();
        }

        public Task<List<HoatDong>> LayThongBaoAsync(int soLuong) =>
            _db.HoatDongs.Where(h => h.LaThongBao && !h.DaDoc)
                .OrderByDescending(h => h.ThoiGian)
                .Take(soLuong)
                .ToListAsync();

        public Task<int> DemChuaDocAsync() =>
            _db.HoatDongs.CountAsync(h => h.LaThongBao && !h.DaDoc);

        public async Task DanhDauDaDocAsync()
        {
            var chuaDoc = await _db.HoatDongs.Where(h => h.LaThongBao && !h.DaDoc).ToListAsync();
            foreach (var h in chuaDoc) h.DaDoc = true;
            await _db.SaveChangesAsync();
        }
    }
}
