using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface IChiTietDatPhongRepository : IGenericRepository<ChiTietDatPhong>
    {
        Task<ChiTietDatPhong?> LayTheoMaAsync(int id);
        Task ThemAsync(ChiTietDatPhong chiTiet);
        Task CapNhatAsync(ChiTietDatPhong chiTiet);

        // ============ Thống kê doanh thu (Module 5 - Dashboard & báo cáo) ============
        // Các hàm dưới đây trả về luôn kết quả tính toán để Controller
        // không phải tự truy cập DbContext / viết lại truy vấn LINQ.

        // Có giao dịch chi tiết đặt phòng nào trong hệ thống hay không.
        Task<bool> CoGiaoDichAsync();

        // Tổng doanh thu của toàn bộ chi tiết còn hiệu lực (không tính chi tiết đã hủy).
        Task<decimal> TongDoanhThuHieuLucAsync();

        // Doanh thu của chi tiết còn hiệu lực trong tháng/năm chỉ định.
        Task<decimal> DoanhThuThangHieuLucAsync(int nam, int thang);

        // Doanh thu của chi tiết còn hiệu lực trong kỳ tháng/năm chỉ định.
        Task<decimal> DoanhThuKyHieuLucAsync(int nam, int thang);

        // Số lượt đặt còn hiệu lực trong kỳ tháng/năm chỉ định.
        Task<int> DemLuotDatHieuLucTrongKyAsync(int nam, int thang);

        // Số lượt đặt đã hủy trong kỳ tháng/năm chỉ định.
        Task<int> DemLuotHuyTrongKyAsync(int nam, int thang);

        // Danh sách năm có phát sinh giao dịch (dùng cho ô chọn năm của báo cáo).
        Task<List<int>> LayDanhSachNamCoGiaoDichAsync();

        // Doanh thu và số lượt đặt theo từng tháng trong năm (chỉ tính chi tiết còn hiệu lực).
        Task<List<DoanhThuThangItem>> ThongKeDoanhThuTheoThangAsync(int nam);

        // Doanh thu theo hạng phòng trong kỳ tháng/năm chỉ định.
        Task<List<LoaiPhongThongKeItem>> ThongKeDoanhThuTheoLoaiTrongKyAsync(int nam, int thang);

        // Top phòng được đặt nhiều nhất (toàn bộ thời gian).
        Task<List<PhongThongKeItem>> ThongKePhongDatNhieuNhatAsync(int soPhongToiDa);

        // Top phòng được đặt nhiều nhất trong kỳ tháng/năm chỉ định.
        Task<List<PhongThongKeItem>> ThongKePhongDatNhieuNhatTrongKyAsync(int nam, int thang, int soPhongToiDa);

        // ============ Nghiệp vụ đặt phòng (Module 3 & 4) ============
        // Phòng đã có chi tiết đặt phòng trùng khoảng [ngayNhan, ngayTraDuKien]
        // (đối chiếu theo ngày nhận / ngày trả ghi trực tiếp trên chi tiết).
        Task<bool> PhongDaDuocDatTrongKhoangAsync(int maPhong, DateTime ngayNhan, DateTime ngayTraDuKien);

        // Phòng đã có đơn khác trùng khoảng lưu trú (đối chiếu theo ngày nhận / ngay trả dự kiến của đơn).
        Task<bool> PhongDaDuocDatTrongKhoangTheoDonAsync(int maPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien);

        // Số phòng đã bị đặt trong khoảng lưu trú của một loại phòng (bỏ qua đơn đang xử lý).
        Task<int> DemPhongDaDatTrongKhoangAsync(int maLoaiPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien);

        // Danh sách mã phòng đã bị đặt trong khoảng lưu trú của một loại phòng.
        Task<List<int>> LayDanhSachPhongDaDatTrongKhoangAsync(int maLoaiPhong, int maDatPhong, DateTime ngayNhan, DateTime ngayTraDuKien);

        // Lịch sử đặt phòng chưa hủy của khách hàng (kèm phòng đã chọn).
        Task<List<ChiTietDatPhong>> LayLichSuTheoKhachHangAsync(int maKhachHang);
    }
}
