using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Repositories.Interfaces
{
    public interface IDatPhongRepository : IGenericRepository<DatPhong>
    {
        Task<DatPhong?> LayTheoMaVoiChiTietAsync(int id);
        Task CapNhatAsync(DatPhong datPhong);

        // ============ Thống kê đơn đặt phòng (Module 5 - Dashboard & báo cáo) ============
        // Tổng số đơn đặt phòng trong hệ thống.
        Task<int> DemDatPhongAsync();

        // Đếm số đơn đặt phòng theo trạng thái.
        Task<int> DemDatPhongTheoTrangThaiAsync(string trangThai);

        // Số đơn đặt phòng mới nhất (kèm khách hàng và chi tiết phòng đã đặt).
        Task<List<DatPhong>> LayDatPhongGanNhatAsync(int soLuong);

        // Các đơn nhận phòng trong kỳ tháng/năm chỉ định (kèm khách hàng).
        Task<List<DatPhong>> LayDatPhongNhanPhongTrongKyAsync(int nam, int thang);

        // Số lượt đặt theo từng tháng trong năm (bao gồm cả đơn đã hủy).
        Task<List<DoanhThuThangItem>> ThongKeLuotDatTheoThangAsync(int nam);

        // ============ Nghiệp vụ đặt phòng (Module 3 & 4) ============
        // Thêm mới một đơn đặt phòng (kèm lưu thay đổi).
        Task ThemAsync(DatPhong datPhong);

        // Đơn đặt phòng có tồn tại trong hệ thống hay không.
        Task<bool> TonTaiAsync(int maDatPhong);

        // Chi tiết đơn đặt phòng của một khách hàng (kèm khách hàng, tài khoản, phòng và loại phòng).
        Task<DatPhong?> LayChiTietCuaKhachHangAsync(int maDatPhong, int maKhachHang);

        // Đơn đặt phòng của một khách hàng dùng để hủy (kèm chi tiết và phòng đã chọn).
        Task<DatPhong?> LayDeHuyCuaKhachHangAsync(int maDatPhong, int maKhachHang);

        // Đơn đặt phòng kèm khách hàng, chi tiết, phòng và loại phòng.
        Task<DatPhong?> LayTheoMaVoiKhachHangVaChiTietAsync(int maDatPhong);

        // Đơn đặt phòng kèm chi tiết và phòng đã chọn.
        Task<DatPhong?> LayTheoMaVoiChiTietVaPhongAsync(int maDatPhong);

        // Đơn đặt phòng kèm chi tiết, phòng và loại phòng.
        Task<DatPhong?> LayTheoMaVoiChiTietVaLoaiPhongAsync(int maDatPhong);

        // Tìm kiếm / lọc / sắp xếp / phân trang danh sách đơn tiếp nhận (tự giới hạn trang vượt quá tổng số trang).
        Task<(List<DatPhong> DanhSach, int TongBanGhi, int TrangHienTai, int TongSoTrang)> TimKiemDatPhongAsync(string? tuKhoa, string? trangThai, int? maLoaiPhong, DateTime? tuNgay, DateTime? denNgay, string? sapXep, int trang, int pageSize);

        // Lịch sử đặt phòng của khách hàng (đã lọc, sắp xếp và phân trang).
        Task<List<DatPhongItemViewModel>> LayLichSuCuaKhachHangAsync(int maKhachHang, string? trangThaiFilter, string sortOrder, int skip, int take);

        // Tổng số đơn đặt phòng của khách hàng (đã áp dụng bộ lọc trạng thái nếu có).
        Task<int> DemLichSuCuaKhachHangAsync(int maKhachHang, string? trangThaiFilter);

        // Thống kê số đơn đặt phòng của khách hàng theo từng trạng thái.
        Task<Dictionary<string, int>> ThongKeTrangThaiCuaKhachHangAsync(int maKhachHang);
    }
}
