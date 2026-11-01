using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>Briefing buổi làm việc do "quản gia AI" tổng hợp từ dữ liệu thật.</summary>
    public class AdminBriefingViewModel
    {
        public string LoiChao { get; set; } = string.Empty;
        public List<string> ThongDiep { get; set; } = new();
        public List<string> ViecUuTien { get; set; } = new();
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

            vm.ThongDiep.Add($"🏨 Công suất: {phongCoKhach}/{phongChoThue} phòng đang có khách ({tyLe}%), còn {phongTrong} phòng trống{($", {phongBaoTri} phòng bảo trì")}.");
            vm.ThongDiep.Add($"🛎️ Hôm nay: {denHomNay} đơn nhận phòng, {traChuaTra + traRoi} đơn trả phòng ({traRoi} đã trả, {traChuaTra} chưa).");
            vm.ThongDiep.Add(donChoXuLy > 0
                ? $"📋 {donChoXuLy} đơn CHỜ XỬ LÝ{(choQua24h > 0 ? $" — trong đó {choQua24h} đơn chờ quá 24h, xử lý gấp nhé!" : ".")}"
                : "✅ Không còn đơn chờ xử lý — quầy đang thông thoáng.");
            vm.ThongDiep.Add($"💰 Doanh thu tháng này đến nay: {doanhThuThang.ToString("N0", t)}₫ từ {luotThang} lượt đặt.");
            if (donMoiHomNay > 0)
            {
                vm.ThongDiep.Add($"🆕 Hôm nay đã tiếp nhận {donMoiHomNay} đơn mới.");
            }
            if (phongTrong <= 2)
            {
                vm.ThongDiep.Add($"⚠️ Chỉ còn {phongTrong} phòng trống — khách hỏi đặt sẽ nhanh hết, cân nhắc báo giá sớm.");
            }

            // ===== AI chốt việc ưu tiên: xếp theo mức độ khẩn, việc nào gấp nằm trên =====
            if (choQua24h > 0)
            {
                vm.ViecUuTien.Add($"🔴 Chờ xử lý gấp: {choQua24h} đơn đặt quá 24h chưa xác nhận — khách dễ hủy, vào \"Tiếp nhận & trạng thái\" ngay.");
            }
            var nhanHomNayChuaXacNhan = tatCa.Count(d => d.NgayNhan.Date == homNay && d.TrangThai is "ChoXuLy" or "DangXuLy");
            if (nhanHomNayChuaXacNhan > 0)
            {
                vm.ViecUuTien.Add($"🟠 {nhanHomNayChuaXacNhan} đơn nhận phòng HÔM NAY chưa xác nhận — liên hệ khách trước giờ check-in 14:00.");
            }
            var dgTieuCuc = await _danhGiaService.DemTieuCucChuaPhanHoiAsync();
            if (dgTieuCuc > 0)
            {
                vm.ViecUuTien.Add($"🟠 {dgTieuCuc} đánh giá TIÊU CỰC còn nợ phản hồi — khách đang chờ, vào \"Đánh giá khách hàng\" trả lời ngay.");
            }
            if (traChuaTra > 0)
            {
                vm.ViecUuTien.Add($"🟡 {traChuaTra} phòng cần trả trước 12:00 — nhắc khách trả phòng để dọn dẹp.");
            }
            if (phongTrong == 0)
            {
                vm.ViecUuTien.Add("🟢 Đã kín phòng — tập trung phục vụ khách hiện tại, chặn trùng lịch.");
            }
            else if (phongTrong <= 2)
            {
                vm.ViecUuTien.Add($"🟢 Chỉ còn {phongTrong} phòng — chào giá ngay khi có khách hỏi để không lỡ đơn.");
            }
            if (vm.ViecUuTien.Count == 0)
            {
                vm.ViecUuTien.Add("✅ Không có việc gấp — vận hành ổn định, tranh thủ rà soát tuần này nhé.");
            }

            return vm;
        }
    }
}
