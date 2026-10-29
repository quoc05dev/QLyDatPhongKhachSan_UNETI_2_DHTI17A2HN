using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public EmailService(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task GuiXacNhanDatPhongThanhCongAsync(int maDatPhong)
        {
            var smtp = _config.GetSection("Smtp");
            string host = smtp["Host"] ?? "smtp.gmail.com";
            int port = int.TryParse(smtp["Port"], out int p) ? p : 587;
            bool enableSsl = bool.TryParse(smtp["EnableSsl"], out bool ssl) ? ssl : true;
            string username = smtp["Username"] ?? string.Empty;
            string password = smtp["Password"] ?? string.Empty;
            string fromEmail = smtp["FromEmail"] ?? username;
            string fromName = smtp["FromName"] ?? "UNETI Grand Hotel";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var datPhong = await _context.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(ph => ph!.LoaiPhong)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null) return;
            var email = datPhong.KhachHang?.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(datPhong.KhachHang?.HoTen ?? email, email));
            message.Subject = $"[UNETI Grand Hotel] Xác nhận đặt phòng thành công #{datPhong.MaDatPhong:D4}";

            var hoTenHtml = WebUtility.HtmlEncode(datPhong.KhachHang?.HoTen ?? string.Empty);
            var trangThaiHtml = WebUtility.HtmlEncode(datPhong.TrangThai);

            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family:Arial,sans-serif;line-height:1.6;color:#222;'>");
            sb.AppendLine($"<h2>Xác nhận đặt phòng thành công</h2>");
            sb.AppendLine($"<p>Xin chào <strong>{hoTenHtml}</strong>,</p>");
            sb.AppendLine($"<p>Chúng tôi xác nhận đơn đặt phòng của quý khách đã được ghi nhận thành công.</p>");
            sb.AppendLine("<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse;border-color:#ddd;min-width:400px;'>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Mã đơn đặt phòng</strong></td><td>#{datPhong.MaDatPhong:D4}</td></tr>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Ngày đặt</strong></td><td>{datPhong.NgayDat:dd/MM/yyyy HH:mm}</td></tr>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Ngày nhận phòng</strong></td><td>{datPhong.NgayNhan:dd/MM/yyyy}</td></tr>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Ngày trả dự kiến</strong></td><td>{datPhong.NgayTraDuKien:dd/MM/yyyy}</td></tr>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Số người</strong></td><td>{datPhong.SoNguoi}</td></tr>");
            sb.AppendLine($"<tr><td style='background:#f8f9fa;'><strong>Trạng thái</strong></td><td>{trangThaiHtml}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<h3>Chi tiết phòng</h3>");
            sb.AppendLine("<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse;border-color:#ddd;min-width:400px;'>");
            sb.AppendLine("<tr style='background:#f8f9fa;'><th>Phòng</th><th>Loại phòng</th><th>Số đêm</th><th>Đơn giá</th><th>Thành tiền</th></tr>");
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                var soPhongHtml = WebUtility.HtmlEncode(ct.Phong?.SoPhong ?? string.Empty);
                var tenLoaiHtml = WebUtility.HtmlEncode(ct.Phong?.LoaiPhong?.TenLoai ?? string.Empty);
                sb.AppendLine($"<tr><td>{soPhongHtml}</td><td>{tenLoaiHtml}</td><td>{ct.SoNgay}</td><td>{ct.DonGia:N0}đ</td><td>{ct.ThanhTien:N0}đ</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine($"<p><strong>Tổng tiền phòng:</strong> {datPhong.TongTien:N0}đ</p>");
            sb.AppendLine($"<p><strong>Tiền cọc:</strong> {datPhong.TienCoc:N0}đ</p>");
            sb.AppendLine($"<p><strong>Còn lại:</strong> {(datPhong.TongTien - datPhong.TienCoc):N0}đ</p>");
            sb.AppendLine("<p>Quý khách vui lòng đến lễ tân xác nhận và thanh toán theo quy định.</p>");
            sb.AppendLine("<p>Trân trọng,<br/><strong>UNETI Grand Hotel</strong></p>");
            sb.AppendLine("</body></html>");

            message.Body = new TextPart("html") { Text = sb.ToString() };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
