using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Nghiệp vụ đánh giá: khách chấm sao + bình luận cho đơn đã hoàn thành;
    /// AI (CamXucAnalyzer) phân tích cảm xúc và soạn nháp phản hồi cho lễ tân.
    /// </summary>
    public interface IDanhGiaService
    {
        Task<(DanhGia? ketQua, string? loi)> TaoAsync(int maDatPhong, int soSao, string? binhLuan, int maTaiKhoan);
        Task<DanhGia?> LayTheoMaDatPhongAsync(int maDatPhong);
        Task<List<int>> LayCacDonDaDanhGiaAsync(List<int> maDatPhongs);
        Task<List<DanhGia>> LayTatCaAsync(int soLuongToiDa = 200);
        Task CapNhatPhanHoiAsync(int maDanhGia, string phanHoi);
    }

    public class DanhGiaService : IDanhGiaService
    {
        private readonly AppDbContext _db;
        private readonly IHoatDongService _hoatDongService;

        public DanhGiaService(AppDbContext db, IHoatDongService hoatDongService)
        {
            _db = db;
            _hoatDongService = hoatDongService;
        }

        public async Task<(DanhGia? ketQua, string? loi)> TaoAsync(int maDatPhong, int soSao, string? binhLuan, int maTaiKhoan)
        {
            if (soSao is < 1 or > 5) return (null, "Số sao phải từ 1 đến 5.");
            var noiDung = (binhLuan ?? string.Empty).Trim();
            if (noiDung.Length == 0) return (null, "Bạn hãy viết vài dòng cảm nhận nhé!");
            if (noiDung.Length > 1000) noiDung = noiDung[..1000];

            var don = await _db.DatPhongs
                .Include(d => d.KhachHang)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (don == null) return (null, "Không tìm thấy đơn đặt phòng.");
            if (don.KhachHang?.MaTaiKhoan != maTaiKhoan) return (null, "Bạn chỉ đánh giá được đơn của chính mình.");
            if (don.TrangThai != "HoanThanh") return (null, "Chỉ đánh giá được sau khi đã trả phòng (đơn hoàn thành).");
            if (await _db.DanhGias.AnyAsync(dg => dg.MaDatPhong == maDatPhong)) return (null, "Đơn này đã được đánh giá rồi.");

            var camXuc = CamXucAnalyzer.PhanTich(noiDung, soSao);
            var danhGia = new DanhGia
            {
                MaDatPhong = maDatPhong,
                TenKhachHang = don.KhachHang.HoTen,
                SoSao = soSao,
                BinhLuan = noiDung,
                TinhCam = camXuc.TinhCam,
                DiemCamXuc = camXuc.Diem,
                GoiYPhanHoi = camXuc.GoiYPhanHoi
            };

            _db.DanhGias.Add(danhGia);
            await _db.SaveChangesAsync();

            // Bắn thông báo lên trang quản trị ngay khi khách đánh giá.
            await _hoatDongService.GhiAsync(
                "DanhGia",
                $"Khách {danhGia.TenKhachHang} đánh giá {soSao}★ (cảm xúc: {TenTinhCam(danhGia.TinhCam)}) cho đơn #{maDatPhong}.",
                maTaiKhoan: maTaiKhoan,
                tenNguoiDung: danhGia.TenKhachHang,
                maLienQuan: maDatPhong,
                laThongBao: true);

            return (danhGia, null);
        }

        public Task<DanhGia?> LayTheoMaDatPhongAsync(int maDatPhong) =>
            _db.DanhGias.FirstOrDefaultAsync(dg => dg.MaDatPhong == maDatPhong);

        public Task<List<int>> LayCacDonDaDanhGiaAsync(List<int> maDatPhongs) =>
            _db.DanhGias.Where(dg => maDatPhongs.Contains(dg.MaDatPhong))
                .Select(dg => dg.MaDatPhong)
                .ToListAsync();

        public async Task<List<DanhGia>> LayTatCaAsync(int soLuongToiDa = 200) =>
            await _db.DanhGias.OrderByDescending(dg => dg.NgayDanhGia)
                .Take(soLuongToiDa)
                .ToListAsync();

        public async Task CapNhatPhanHoiAsync(int maDanhGia, string phanHoi)
        {
            var dg = await _db.DanhGias.FindAsync(maDanhGia)
                ?? throw new InvalidOperationException($"Không tìm thấy đánh giá #{maDanhGia}.");
            dg.PhanHoi = phanHoi.Trim();
            dg.DaPhanHoi = true;
            await _db.SaveChangesAsync();
        }

        public static string TenTinhCam(string tinhCam) => tinhCam switch
        {
            "TichCuc" => "Tích cực",
            "TieuCuc" => "Tiêu cực",
            _ => "Trung lập"
        };
    }
}
