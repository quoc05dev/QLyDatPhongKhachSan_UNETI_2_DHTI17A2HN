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
        private readonly IAdminBriefingService _adminBriefingService;

        public ChatbotService(
            ILoaiPhongRepository loaiPhongRepository,
            IPhongRepository phongRepository,
            IDichVuRepository dichVuRepository,
            IDatPhongRepository datPhongRepository,
            IKhachHangRepository khachHangRepository,
            IAdminBriefingService adminBriefingService)
        {
            _loaiPhongRepository = loaiPhongRepository;
            _phongRepository = phongRepository;
            _dichVuRepository = dichVuRepository;
            _datPhongRepository = datPhongRepository;
            _khachHangRepository = khachHangRepository;
            _adminBriefingService = adminBriefingService;
        }

        public bool DangDungAi => false;

        public async Task<string> HoiAsync(
            string cauHoi,
            int? maTaiKhoan,
            string? vaiTro = null,
            string? hoTen = null,
            IReadOnlyList<ChatLuot>? lichSu = null)
        {
            var t = BoDau(BoDauMoi(cauHoi)).ToLowerInvariant();
            var laNhanVien = vaiTro is "Admin" or "NhanVien";

            // 0) Trợ lý vận hành (chỉ Admin/Nhân viên): bản tin mới nhất của hệ thống
            if (laNhanVien && (t.Contains("tinh hinh") || t.Contains("hom nay") || t.Contains("bao cao")
                || t.Contains("tong quan") || t.Contains("cong viec") || t.Contains("cho xu ly")
                || t.Contains("tin moi") || t.Contains("hien trang") || t.Contains("viec gi")
                || t.Contains("uu tien") || t.Contains("lam truoc")))
            {
                var briefing = await _adminBriefingService.TaoBriefingAsync(hoTen);
                return DinhDangBriefing(briefing);
            }

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

            // 1b) Hai người đặt cùng lúc / đặt trùng
            if (t.Contains("cung luc") || t.Contains("dong thoi") || t.Contains("dat trung")
                || t.Contains("tranh chap"))
            {
                return "Hệ thống khoá phòng ngay khi tạo đơn nên 2 người bấm đặt cùng lúc chỉ 1 đơn được ghi nhận; người còn lại được báo chọn phòng hoặc ngày khác. "
                     + "Nếu phát sinh trùng, khách sạn ưu tiên đơn đã cọc/thanh toán trước; đơn sau được đổi phòng tương đương hoặc hoàn cọc. Lễ tân sẽ liên hệ để xử lý.";
            }

            // 1c) Nâng hạng phòng / thanh toán chênh lệch
            if (t.Contains("nang hang") || t.Contains("nang cap") || t.Contains("hang cao hon"))
            {
                return "Khách đã đặt phòng muốn nâng hạng: liên hệ lễ tân để đổi sang phòng hạng cao hơn nếu còn trống. "
                     + "Phần chênh lệch được cộng vào đơn và thu tại quầy khi đổi, hoặc thanh toán cùng hoá đơn khi trả phòng. "
                     + "Nếu đã trả trước, hệ thống ghi nhận thêm khoản chênh lệch chứ không hoàn lại phần đã trả.";
            }

            // 1d) Bảo trì phòng
            if (t.Contains("bao tri") || t.Contains("sua chua") || (t.Contains("khi nao") && t.Contains("xong")))
            {
                return await TraLoiBaoTriAsync(laNhanVien);
            }

            // 1e) Khiếu nại dịch vụ
            if (t.Contains("khieu nai") || t.Contains("phan nan") || t.Contains("khong hai long")
                || (t.Contains("chat luong") && (t.Contains("kem") || t.Contains("te"))))
            {
                return "Khiếu nại dịch vụ:\n"
                     + "- Đang lưu trú: gọi hotline 1900 6068 hoặc email support@uhotel.click, lễ tân tiếp nhận ngay.\n"
                     + "- Sau khi trả phòng: gửi đánh giá trong \"Lịch sử đặt phòng\".\n"
                     + "- Lễ tân/CSKH tiếp nhận, quản lý khách sạn xử lý và phản hồi trong 24 giờ.";
            }

            // 1f) Khuyến mãi / khách quen
            if (t.Contains("sale") || t.Contains("giam gia") || t.Contains("khuyen mai") || t.Contains("uu dai")
                || t.Contains("sinh nhat") || t.Contains("ngay le") || t.Contains("ki niem")
                || t.Contains("khach quen") || t.Contains("dat nhieu lan"))
            {
                return "Hiện khách sạn chưa có chương trình giảm giá cho ngày lễ, kỷ niệm, sinh nhật hay khách đặt phòng nhiều lần. "
                     + "Mình đã ghi nhận đề xuất của bạn; khi có chương trình sẽ thông báo trên website. "
                     + "Bạn có thể để lại số điện thoại/email để lễ tân tư vấn khi có ưu đãi mới.";
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

            // 9) Lời chào — nhân viên được chào kèm bản tin, khách được chào chung
            if (t.Contains("chao") || t.Contains("hello") || t.Contains("hi ") || t == "hi")
            {
                if (laNhanVien)
                {
                    var briefing = await _adminBriefingService.TaoBriefingAsync(hoTen);
                    return DinhDangBriefing(briefing)
                         + "\n\nBạn hỏi thêm \"hôm nay có gì\" để cập nhật mới nhất nhé!";
                }
                return "Xin chào! Mình là trợ lý ảo của UNETI Grand. Bạn hỏi mình về giá phòng, phòng trống, dịch vụ, chính sách hay đơn đặt phòng nhé!";
            }

            if (laNhanVien)
            {
                return "Mình là trợ lý vận hành của khách sạn. Bạn thử hỏi:\n"
                     + "• \"Hôm nay có gì?\" — bản tin: đơn chờ xử lý, khách đến/đi, phòng trống\n"
                     + "• \"Giá phòng bao nhiêu?\"\n• \"Đơn #2031\"";
            }

            return "Mình chưa hiểu câu hỏi 😅 Bạn thử một trong các chủ đề sau:\n"
                 + "• \"Giá phòng bao nhiêu?\"\n• \"Còn phòng trống không?\"\n• \"Có dịch vụ gì?\"\n"
                 + "• \"Chính sách hủy thế nào?\"\n• \"Đơn #2031 của tôi tới đâu?\"";
        }

        public async Task<string> TaoDuKienAsync(int? maTaiKhoan, string? vaiTro = null, string? hoTen = null)
        {
            var vn = CultureInfo.GetCultureInfo("vi-VN");
            var sb = new StringBuilder();
            sb.AppendLine($"Hôm nay: {DateTime.Now:dd/MM/yyyy HH:mm}.");

            var loais = await _loaiPhongRepository.LayTatCaHoatDongAsync();
            if (loais.Any())
            {
                sb.AppendLine("Giá phòng: " + string.Join("; ", loais.Select(l =>
                    $"{l.TenLoai} {l.DonGiaNgay.ToString("N0", vn)}đ/đêm (tối đa {l.SoNguoiToiDa} khách)")) + ".");
            }

            var phongs = await _phongRepository.LayDanhSachPhongTrongAsync();
            sb.AppendLine($"Phòng đang trống: {phongs.Count}.");

            var dichVus = (await _dichVuRepository.TimKiem(null)).Where(d => d.TrangThai).ToList();
            if (dichVus.Any())
            {
                sb.AppendLine("Dịch vụ: " + string.Join("; ", dichVus.Take(8).Select(d =>
                    $"{d.TenDichVu} {d.Gia.ToString("N0", vn)}đ")) + ".");
            }

            var laNhanVien = vaiTro is "Admin" or "NhanVien";
            var phongBaoTri = (await _phongRepository.LayTatCaAsync())
                .Where(p => p.TrangThai == "BaoTri")
                .OrderBy(p => p.SoPhong)
                .ToList();
            if (phongBaoTri.Any())
            {
                sb.AppendLine("Phòng đang bảo trì: " + string.Join("; ", phongBaoTri.Select(p =>
                    $"phòng {p.SoPhong} ({(p.NgayBaoTri.HasValue ? $"từ {p.NgayBaoTri:dd/MM/yyyy}" : "chưa rõ ngày bắt đầu")}, "
                    + $"{(p.NgayHoanThanhBaoTri.HasValue ? $"dự kiến xong {p.NgayHoanThanhBaoTri:dd/MM/yyyy}" : "chưa có ngày sửa xong")}"
                    + $"{(laNhanVien && !string.IsNullOrWhiteSpace(p.GhiChuBaoTri) ? $", ghi chú: {p.GhiChuBaoTri}" : "")})")) + ".");
            }
            else
            {
                sb.AppendLine("Hiện không có phòng nào đang bảo trì.");
            }

            if (maTaiKhoan is not null)
            {
                var khachDangHoi = await _khachHangRepository.LayTheoMaTaiKhoanAsync(maTaiKhoan.Value);
                if (khachDangHoi != null)
                {
                    var thongKeKhach = await _datPhongRepository.ThongKeTrangThaiCuaKhachHangAsync(khachDangHoi.MaKhachHang);
                    var soLanDaO = thongKeKhach.GetValueOrDefault("HoanThanh");
                    sb.AppendLine($"Khách đang hỏi đã hoàn thành {soLanDaO} lần lưu trú.");
                }
            }

            sb.AppendLine("Địa chỉ: UNETI Grand Hotel & Spa, Hà Nội. Hotline 1900 6068, email support@uhotel.click.");
            sb.AppendLine("Check-in từ 14:00, check-out trước 12:00. Hủy miễn phí trước 24 giờ, hủy muộn phí 1 đêm đầu; đổi phòng cùng loại miễn phí nếu còn trống.");
            sb.AppendLine("Chính sách vận hành:");
            sb.AppendLine("- Đặt trùng giờ: hệ thống khoá phòng khi tạo đơn nên đơn thứ hai bị từ chối; nếu phát sinh trùng, ưu tiên đơn đã cọc/thanh toán trước, đơn còn lại được đổi phòng tương đương hoặc hoàn cọc.");
            sb.AppendLine("- Nâng hạng phòng: đổi sang phòng hạng cao hơn nếu còn trống; chênh lệch cộng vào tổng tiền, thu tại quầy khi đổi hoặc thanh toán khi trả phòng.");
            sb.AppendLine("- Bảo trì: phòng bảo trì tạm khoá đặt/đổi; ngày sửa xong lấy theo ngày dự kiến, chưa có thì lễ tân xác nhận.");
            sb.AppendLine("- Khiếu nại dịch vụ: hotline 1900 6068 hoặc email support@uhotel.click khi đang lưu trú; sau khi trả phòng gửi đánh giá trong \"Lịch sử đặt phòng\". Lễ tân/CSKH tiếp nhận, quản lý xử lý và phản hồi trong 24 giờ.");
            sb.AppendLine("- Khuyến mãi: hiện chưa có chương trình giảm giá cho ngày lễ, kỷ niệm, sinh nhật hay khách quen; khách đã ở nhiều lần được lễ tân ghi nhận để chăm sóc.");

            if (vaiTro is "Admin" or "NhanVien")
            {
                var briefing = await _adminBriefingService.TaoBriefingAsync(hoTen);
                sb.AppendLine("Bản tin vận hành cho nhân viên:");
                foreach (var muc in briefing.ViecUuTien.Concat(briefing.ThongDiep))
                {
                    sb.AppendLine("- " + muc.NoiDung);
                }
            }
            else if (maTaiKhoan is not null)
            {
                sb.AppendLine("Khách đã đăng nhập; được phép tra cứu đơn đặt phòng của chính khách này.");
            }
            else
            {
                sb.AppendLine("Khách chưa đăng nhập; không được tra cứu đơn đặt phòng.");
            }

            return sb.ToString();
        }

        private async Task<string> TraLoiBaoTriAsync(bool laNhanVien)
        {
            var phongBaoTri = (await _phongRepository.LayTatCaAsync())
                .Where(p => p.TrangThai == "BaoTri")
                .OrderBy(p => p.SoPhong)
                .ToList();

            if (!phongBaoTri.Any())
            {
                return "Hiện không có phòng nào đang bảo trì. Nếu cần kiểm tra một phòng cụ thể, bạn cho mình số phòng nhé.";
            }

            var sb = new StringBuilder("Phòng đang bảo trì:\n");
            foreach (var p in phongBaoTri)
            {
                var batDau = p.NgayBaoTri.HasValue ? $"từ {p.NgayBaoTri:dd/MM/yyyy}" : "chưa ghi ngày bắt đầu";
                var xong = p.NgayHoanThanhBaoTri.HasValue
                    ? $"dự kiến xong {p.NgayHoanThanhBaoTri:dd/MM/yyyy}"
                    : "chưa có ngày xong cụ thể";
                sb.AppendLine($"- Phòng {p.SoPhong}: {batDau}, {xong}.");
                if (laNhanVien && !string.IsNullOrWhiteSpace(p.GhiChuBaoTri))
                {
                    sb.AppendLine($"  Ghi chú nội bộ: {p.GhiChuBaoTri}");
                }
            }
            sb.Append("Phòng bảo trì tạm khoá đặt/đổi; khi chưa có ngày xong, lễ tân sẽ xác nhận với bộ phận kỹ thuật.");
            return sb.ToString();
        }

        private static string DinhDangBriefing(AdminBriefingViewModel b)
        {
            return b.LoiChao
                 + "\n\nViệc cần làm trước:\n" + string.Join("\n", b.ViecUuTien.Select(v => "  " + v.NoiDung))
                 + "\n\nTình hình:\n" + string.Join("\n", b.ThongDiep.Select(d => "  • " + d.NoiDung));
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


