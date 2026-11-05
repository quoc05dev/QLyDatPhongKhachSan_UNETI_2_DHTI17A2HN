using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>Một dòng bản tin: nội dung + link tới màn hình xử lý (nếu có).</summary>
    public record BriefingMuc(string NoiDung, string? Url = null);

    /// <summary>Briefing buổi làm việc do "quản gia AI" tổng hợp từ dữ liệu thật.</summary>
    public class AdminBriefingViewModel
    {
        public string LoiChao { get; set; } = string.Empty;
        public List<BriefingMuc> ThongDiep { get; set; } = new();
        public List<BriefingMuc> ViecUuTien { get; set; } = new();
        public int DonChoXuLy { get; set; }
        public int PhongTrong { get; set; }
    }

    /// <summary>
    /// Quản gia AI cho Admin/Nhân viên: mỗi lần mở Dashboard (hoặc hỏi chatbot
    /// "hôm nay có gì") là có ngay bản tin mới nhất — đơn chờ xử lý, khách
    /// đến/đi hôm nay, công suất phòng, doanh thu tháng, cảnh báo phòng sắp kín.
    /// </summary>
    public interface IAdminBriefingService
    {
        Task<AdminBriefingViewModel> TaoBriefingAsync(string? tenNguoiDung);
    }

    public class AdminBriefingService : IAdminBriefingService
    {
        private readonly IDatPhongRepository _datPhongRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly IChiTietDatPhongRepository _chiTietDatPhongRepository;
        private readonly IDanhGiaService _danhGiaService;

        public AdminBriefingService(
            IDatPhongRepository datPhongRepository,
            IPhongRepository phongRepository,
            IChiTietDatPhongRepository chiTietDatPhongRepository,
            IDanhGiaService danhGiaService)
        {
            _datPhongRepository = datPhongRepository;
            _phongRepository = phongRepository;
            _chiTietDatPhongRepository = chiTietDatPhongRepository;
            _danhGiaService = danhGiaService;
        }

        public async Task<AdminBriefingViewModel> TaoBriefingAsync(string? tenNguoiDung)
        {
            var now = DateTime.Now;
            var homNay = now.Date;

            // Gom đơn của tháng này + tháng trước để đếm khách đến/đi hôm nay.
            // shortcut: don tren 2 thang cu se khong vao dem "hom nay"; upgrade:
            // viet repo query theo ngay truc tiep neu co don cu hon 60 ngay.
            var thangNay = await _datPhongRepository.LayDatPhongNhanPhongTrongKyAsync(now.Year, now.Month);
            var thangTruoc = now.Month == 1
                ? await _datPhongRepository.LayDatPhongNhanPhongTrongKyAsync(now.Year - 1, 12)
                : await _datPhongRepository.LayDatPhongNhanPhongTrongKyAsync(now.Year, now.Month - 1);
            var tatCa = thangNay.Concat(thangTruoc).ToList();

            var phongs = await _phongRepository.LayTatCaAsync();
            var phongTrong = phongs.Count(p => p.TrangThai == "Trong");
            var phongCoKhach = phongs.Count(p => p.TrangThai == "DangSuDung");
            var phongBaoTri = phongs.Count(p => p.TrangThai == "BaoTri");
            var phongChoThue = phongs.Count(p => p.TrangThai != "BaoTri");

            var donChoXuLy = await _datPhongRepository.DemDatPhongTheoTrangThaiAsync("ChoXuLy");
            var choQua24h = tatCa.Count(d => d.TrangThai == "ChoXuLy" && d.NgayDat < now.AddHours(-24));

            var denHomNay = tatCa.Count(d => d.NgayNhan.Date == homNay && d.TrangThai is "DaXacNhan" or "DangSuDung");
            var traChuaTra = tatCa.Count(d => d.NgayTraDuKien.Date == homNay && d.TrangThai == "DangSuDung");
            var traRoi = tatCa.Count(d => d.NgayTraDuKien.Date == homNay && d.TrangThai == "HoanThanh");
            var donMoiHomNay = tatCa.Count(d => d.NgayDat.Date == homNay);

            var doanhThuThang = await _chiTietDatPhongRepository.DoanhThuThangHieuLucAsync(now.Year, now.Month);
            var luotThang = await _chiTietDatPhongRepository.DemLuotDatHieuLucTrongKyAsync(now.Year, now.Month);

            var buoi = now.Hour < 5 ? "đêm khuya"
                : now.Hour < 12 ? "buổi sáng"
                : now.Hour < 18 ? "buổi chiều" : "buổi tối";
            var ten = string.IsNullOrWhiteSpace(tenNguoiDung) ? "anh/chị" : tenNguoiDung.Trim();

            var t = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            var tyLe = phongChoThue == 0 ? 0 : Math.Round((double)phongCoKhach / phongChoThue * 100, 0);

            var vm = new AdminBriefingViewModel
            {
                LoiChao = $"Chào {buoi}, {ten}! Hôm nay {now:dd/MM/yyyy} — bản tin vận hành dành cho bạn.",
                DonChoXuLy = donChoXuLy,
                PhongTrong = phongTrong
            };

            const string urlDonCho = "/QuanLyDatPhong?trangThai=ChoXuLy";
            const string urlPhongTrong = "/Phong?trangThai=Trong";

            vm.ThongDiep.Add(new($"Công suất: {phongCoKhach}/{phongChoThue} phòng đang có khách ({tyLe}%), còn {phongTrong} phòng trống{($", {phongBaoTri} phòng bảo trì")}.", "/Phong"));
            vm.ThongDiep.Add(new($"Hôm nay: {denHomNay} đơn nhận phòng, {traChuaTra + traRoi} đơn trả phòng ({traRoi} đã trả, {traChuaTra} chưa).", "/QuanLyDatPhong"));
            vm.ThongDiep.Add(donChoXuLy > 0
                ? new($"{donChoXuLy} đơn CHỜ XỬ LÝ{(choQua24h > 0 ? $" — trong đó {choQua24h} đơn chờ quá 24h, xử lý gấp nhé!" : ".")}", urlDonCho)
                : new("Không còn đơn chờ xử lý — quầy đang thông thoáng.", "/QuanLyDatPhong"));
            vm.ThongDiep.Add(new($"Doanh thu tháng này đến nay: {doanhThuThang.ToString("N0", t)}₫ từ {luotThang} lượt đặt.", "/ThongKe/DoanhThu"));
            if (donMoiHomNay > 0)
            {
                vm.ThongDiep.Add(new($"Hôm nay đã tiếp nhận {donMoiHomNay} đơn mới.", "/QuanLyDatPhong"));
            }
            if (phongTrong <= 2)
            {
                vm.ThongDiep.Add(new($"Chỉ còn {phongTrong} phòng trống — khách hỏi đặt sẽ nhanh hết, cân nhắc báo giá sớm.", urlPhongTrong));
            }

            // Phòng bảo trì quá ngày dự kiến sửa xong: nhắc lễ tân cập nhật/đẩy tiến độ.
            var baoTriQuaHan = phongs
                .Where(p => p.TrangThai == "BaoTri"
                    && p.NgayHoanThanhBaoTri.HasValue
                    && p.NgayHoanThanhBaoTri.Value.Date < homNay)
                .Select(p => p.SoPhong)
                .ToList();
            if (baoTriQuaHan.Count > 0)
            {
                vm.ThongDiep.Add(new($"{baoTriQuaHan.Count} phòng bảo trì quá hạn sửa xong: {string.Join(", ", baoTriQuaHan)}.", "/Phong?trangThai=BaoTri"));
            }

            // Khách đến hôm nay: sinh nhật / khách quen để lễ tân chủ động chăm sóc.
            var khachDenHomNay = tatCa
                .Where(d => d.NgayNhan.Date == homNay && d.TrangThai is "DaXacNhan" or "DangSuDung")
                .GroupBy(d => d.MaKhachHang)
                .Select(g => g.First())
                .ToList();

            var sinhNhatHomNay = khachDenHomNay
                .Where(d => d.KhachHang?.NgaySinh is { } ns && ns.Month == homNay.Month && ns.Day == homNay.Day)
                .Select(d => d.KhachHang!.HoTen)
                .Distinct()
                .ToList();
            if (sinhNhatHomNay.Count > 0)
            {
                vm.ViecUuTien.Add(new($"Sinh nhật khách hôm nay: {string.Join(", ", sinhNhatHomNay)} — gửi lời chúc/ưu đãi nhỏ.", "/QuanLyDatPhong"));
            }

            var khachQuen = new List<string>();
            foreach (var d in khachDenHomNay)
            {
                var thongKeCuaKhach = await _datPhongRepository.ThongKeTrangThaiCuaKhachHangAsync(d.MaKhachHang);
                var soLanODaHoanThanh = thongKeCuaKhach.GetValueOrDefault("HoanThanh");
                if (soLanODaHoanThanh >= 3)
                {
                    khachQuen.Add($"{d.KhachHang?.HoTen} ({soLanODaHoanThanh} lần)");
                }
            }
            if (khachQuen.Count > 0)
            {
                vm.ViecUuTien.Add(new($"Khách quen đến hôm nay: {string.Join(", ", khachQuen)} — chào hỏi và ưu đãi theo chính sách khách sạn.", "/QuanLyDatPhong"));
            }

            // ===== AI chốt việc ưu tiên: xếp theo mức độ khẩn, việc nào gấp nằm trên =====
            if (baoTriQuaHan.Count > 0)
            {
                vm.ViecUuTien.Add(new($"Kiểm tra {baoTriQuaHan.Count} phòng bảo trì quá hạn: {string.Join(", ", baoTriQuaHan)} — cập nhật ngày xong hoặc báo quản lý.", "/Phong?trangThai=BaoTri"));
            }
            if (choQua24h > 0)
            {
                vm.ViecUuTien.Add(new($"Chờ xử lý gấp: {choQua24h} đơn đặt quá 24h chưa xác nhận — khách dễ hủy, xử lý ngay.", urlDonCho));
            }
            var nhanHomNayChuaXacNhan = tatCa.Count(d => d.NgayNhan.Date == homNay && d.TrangThai is "ChoXuLy" or "DangXuLy");
            if (nhanHomNayChuaXacNhan > 0)
            {
                vm.ViecUuTien.Add(new($"{nhanHomNayChuaXacNhan} đơn nhận phòng HÔM NAY chưa xác nhận — liên hệ khách trước giờ check-in 14:00.", urlDonCho));
            }
            var dgTieuCuc = await _danhGiaService.DemTieuCucChuaPhanHoiAsync();
            if (dgTieuCuc > 0)
            {
                vm.ViecUuTien.Add(new($"{dgTieuCuc} đánh giá TIÊU CỰC còn nợ phản hồi — khách đang chờ, trả lời ngay.", "/DanhGia/QuanLy"));
            }
            if (traChuaTra > 0)
            {
                vm.ViecUuTien.Add(new($"{traChuaTra} phòng cần trả trước 12:00 — nhắc khách trả phòng để dọn dẹp.", "/QuanLyDatPhong"));
            }
            if (phongTrong == 0)
            {
                vm.ViecUuTien.Add(new("Đã kín phòng — tập trung phục vụ khách hiện tại, chặn trùng lịch.", urlPhongTrong));
            }
            else if (phongTrong <= 2)
            {
                vm.ViecUuTien.Add(new($"Chỉ còn {phongTrong} phòng — chào giá ngay khi có khách hỏi để không lỡ đơn.", urlPhongTrong));
            }
            if (vm.ViecUuTien.Count == 0)
            {
                vm.ViecUuTien.Add(new("Không có việc gấp — vận hành ổn định, tranh thủ rà soát tuần này nhé."));
            }

            return vm;
        }
    }
}


