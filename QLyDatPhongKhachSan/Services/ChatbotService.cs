using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Chatbot rule-based: map câu hỏi tiếng Việt (có/không dấu) sang truy vấn
    /// repository thật rồi ghép câu trả lời mẫu. Không hallucination vì mọi con
    /// số đều lấy từ database.
    /// </summary>
    public partial class ChatbotService : IChatbotService
    {
        private readonly ILoaiPhongRepository _loaiPhongRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly IDichVuRepository _dichVuRepository;
        private readonly IDatPhongRepository _datPhongRepository;
        private readonly IKhachHangRepository _khachHangRepository;

        public ChatbotService(
            ILoaiPhongRepository loaiPhongRepository,
            IPhongRepository phongRepository,
            IDichVuRepository dichVuRepository,
            IDatPhongRepository datPhongRepository,
            IKhachHangRepository khachHangRepository)
        {
            _loaiPhongRepository = loaiPhongRepository;
            _phongRepository = phongRepository;
            _dichVuRepository = dichVuRepository;
            _datPhongRepository = datPhongRepository;
            _khachHangRepository = khachHangRepository;
        }

        public async Task<string> HoiAsync(string cauHoi, int? maTaiKhoan)
        {
            var t = BoDau(BoDauMoi(cauHoi)).ToLowerInvariant();

            // 1) Truy vấn đơn đặt phòng theo mã (chỉ chủ đơn mới xem được)
            if (t.Contains("don") || t.Contains("ma don") || t.Contains("dat phong cua toi"))
            {
                var so = Regex.Match(cauHoi, @"\d+");
                if (so.Success)
                {
                    return await XemDonAsync(int.Parse(so.Value), maTaiKhoan);
                }
                return "Bạn muốn xem đơn nào? Nhập theo mẫu: \"đơn #2031\". "
                     + "Để bảo mật, chỉ tài khoản đặt đơn mới xem được chi tiết.";
            }

            // 2) Giá cả các hạng phòng
            if (t.Contains("gia") || t.Contains("bao nhieu") || t.Contains("chi phi"))
            {
                var loais = await _loaiPhongRepository.LayTatCaHoatDongAsync();
                if (!loais.Any()) return "Hiện khách sạn chưa cập nhật bảng giá.";
                var sb = new StringBuilder("Bảng giá phòng (theo ngày):\n");
                foreach (var lp in loais)
                {
                    sb.AppendLine($"• {lp.TenLoai}: {lp.DonGiaNgay.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}₫/đêm — tối đa {lp.SoNguoiToiDa} khách.");
                }
                sb.Append("Bạn đặt phòng tại trang \"Đặt phòng\" hoặc nhắn tôi hỗ trợ nhé!");
                return sb.ToString();
            }

            // 3) Phòng còn trống
            if (t.Contains("trong") || t.Contains("con phong") || t.Contains("phong con") || t.Contains("con trống"))
            {
                var phongs = await _phongRepository.LayDanhSachPhongTrongAsync();
                if (!phongs.Any()) return "Hiện tại khách sạn đã kín phòng. Bạn thử chọn ngày khác hoặc để lại SĐT nhé!";
                var ds = string.Join(", ", phongs.Take(8).Select(p => p.SoPhong));
                return $"Hiện có {phongs.Count} phòng đang trống (VD: {ds}{(phongs.Count > 8 ? ", ..." : "")}). "
                     + "Xem chi tiết và đặt ngay tại trang \"Đặt phòng\" nhé!";
            }

            // 4) Dịch vụ đi kèm
            if (t.Contains("dich vu") || t.Contains("dich vu gi") || t.Contains("spa") || t.Contains("an sang") || t.Contains("buoi sang"))
            {
                var dv = (await _dichVuRepository.TimKiem(null)).Where(d => d.TrangThai).ToList();
                if (!dv.Any()) return "Khách sạn chưa có dịch vụ bổ sung nào được bật.";
                var sb = new StringBuilder("Dịch vụ hiện có:\n");
                foreach (var d in dv.Take(8))
                {
                    sb.AppendLine($"• {d.TenDichVu}: {d.Gia.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}₫");
                }
                return sb.ToString().TrimEnd();
            }

            // 5) Chính sách hủy / đổi
            if (t.Contains("huy") || t.Contains("chinh sach") || t.Contains("doi phong") || t.Contains("hoan tien"))
            {
                return "Chính sách khách sạn:\n"
                     + "• Hủy miễn phí trước 24 giờ giờ nhận phòng.\n"
                     + "• Hủy muộn hơn: phí 1 đêm đầu tiên.\n"
                     + "• Đổi phòng cùng loại: miễn phí (còn phòng trống).\n"
                     + "Bạn cần hủy đơn? Vào \"Lịch sử đặt phòng\" và bấm Hủy, hoặc liên hệ lễ tân.";
            }

            // 6) Giờ nhận / trả phòng
            if (t.Contains("gio") || t.Contains("check in") || t.Contains("check-out") || t.Contains("nhan phong") || t.Contains("tra phong"))
            {
                return "Giờ nhận phòng (check-in): từ 14:00.\nGiờ trả phòng (check-out): trước 12:00.\n"
                     + "Nhận sớm / trả muộn phụ thuộc tình trạng phòng, liên hệ lễ tân để được hỗ trợ nhé.";
            }

            // 7) Địa chỉ / liên hệ
            if (t.Contains("dia chi") || t.Contains("lien he") || t.Contains("o dau") || t.Contains("hotline") || t.Contains("sdt ks") || t.Contains("sdt khach san"))
            {
                return "UNETI Grand Hotel & Spa — ký túc xá/cơ sở ĐH Công Nghệ Đông Á (UNETI), Hà Nội.\n"
                     + "Hotel hotline: 1900 6068 · Email: support@uhotel.click\n"
                     + "Bạn hỏi thêm về loại phòng hoặc dịch vụ, tôi hỗ trợ ngay!";
            }

            // 8) Hướng dẫn đặt phòng
            if (t.Contains("dat phong") || t.Contains("dat the nao") || t.Contains("cach dat") || t.Contains("book"))
            {
                return "Cách đặt phòng trong 3 bước:\n1. Chọn hạng phòng + ngày nhận/trả tại trang \"Đặt phòng\".\n"
                     + "2. Nhập thông tin khách và xác nhận.\n3. Đặt cọc online (VNPay) hoặc trả tại quầy.\n"
                     + "Lễ tân sẽ xác nhận trong vài phút!";
            }

            // 9) Lời chào
            if (t.Contains("chao") || t.Contains("hello") || t.Contains("hi ") || t == "hi")
            {
                return "Xin chào! Mình là trợ lý ảo của UNETI Grand. Bạn hỏi mình về giá phòng, phòng trống, dịch vụ, chính sách hay đơn đặt phòng nhé!";
            }

            return "Mình chưa hiểu câu hỏi 😅 Bạn thử một trong các chủ đề sau:\n"
                 + "• \"Giá phòng bao nhiêu?\"\n• \"Còn phòng trống không?\"\n• \"Có dịch vụ gì?\"\n"
                 + "• \"Chính sách hủy thế nào?\"\n• \"Đơn #2031 của tôi tới đâu?\"";
        }

        private async Task<string> XemDonAsync(int maDatPhong, int? maTaiKhoan)
        {
            if (maTaiKhoan is null)
            {
                return "Bạn cần đăng nhập bằng tài khoản đã đặt phòng để xem chi tiết đơn nhé.";
            }

            var khachHang = await _khachHangRepository.LayTheoMaTaiKhoanAsync(maTaiKhoan.Value);
            if (khachHang == null)
            {
                return "Tài khoản của bạn chưa được liên kết hồ sơ khách hàng nên chưa xem được đơn.";
            }

            var don = await _datPhongRepository.LayChiTietCuaKhachHangAsync(maDatPhong, khachHang.MaKhachHang);
            if (don == null)
            {
                return $"Không tìm thấy đơn #{maDatPhong} thuộc tài khoản của bạn. Bạn kiểm tra lại mã đơn nhé.";
            }

            var tenTrangThai = don.TrangThai switch
            {
                "ChoXuLy" => "Chờ xử lý",
                "DangXuLy" => "Đang xử lý",
                "DaXacNhan" => "Đã xác nhận",
                "DangSuDung" => "Đang ở (đã check-in)",
                "HoanThanh" => "Hoàn thành (đã check-out)",
                "DaHuy" => "Đã hủy",
                _ => don.TrangThai
            };

            return $"Đơn #{don.MaDatPhong}:\n"
                 + $"• Nhận phòng: {don.NgayNhan:dd/MM/yyyy} → Trả dự kiến: {don.NgayTraDuKien:dd/MM/yyyy}\n"
                 + $"• Trạng thái: {tenTrangThai}\n"
                 + (don.TongTien > 0 ? $"• Tổng tiền: {don.TongTien.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}₫\n" : "")
                 + "Chi tiết đầy đủ trong mục \"Lịch sử đặt phòng\" nhé!";
        }

        private static string BoDauMoi(string s) =>
            s.Replace("đ", "d").Replace("Đ", "D");

        private static string BoDau(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
