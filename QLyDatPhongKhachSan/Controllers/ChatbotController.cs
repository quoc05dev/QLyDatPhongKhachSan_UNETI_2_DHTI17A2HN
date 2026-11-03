// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung: Chatbot hỏi đáp CSKH (rule-based + LLM bên ngoài khi có API key).

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Services;

namespace QLyDatPhongKhachSan.Controllers
{
    public class ChatbotController : Controller
    {
        private const string KhoaLichSu = "cb-lich-su";
        private const string KhoaDem = "cb-dem";
        private const int SoLuotNguCanh = 12;
        private const int GioiHanMoiPhut = 15;

        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // POST /Chatbot/Hoi - nhan cau hoi dang JSON, tra ve cau tra loi.
        public record ChatbotHoiRequest(string CauHoi);

        [HttpPost]
        public async Task<IActionResult> Hoi([FromBody] ChatbotHoiRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.CauHoi))
            {
                return BadRequest(new { cauTraLoi = "Bạn chưa nhập câu hỏi." });
            }

            if (!ConLuotHoi())
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { cauTraLoi = "Bạn hỏi hơi nhanh, chờ một phút rồi thử lại nhé." });
            }

            // Cau hoi dai bat thuong thi cat ngan (chong spam payload).
            var cauHoi = request.CauHoi.Length > 300 ? request.CauHoi[..300] : request.CauHoi;
            var lichSu = DocLichSu();

            var cauTraLoi = await _chatbotService.HoiAsync(
                cauHoi,
                HttpContext.Session.GetMaTaiKhoan(),
                HttpContext.Session.GetVaiTro(),
                HttpContext.Session.GetHoTen(),
                lichSu);

            lichSu.Add(new ChatLuot("user", cauHoi));
            lichSu.Add(new ChatLuot("assistant", cauTraLoi));
            LuuLichSu(lichSu);

            return Ok(new { cauTraLoi });
        }

        private List<ChatLuot> DocLichSu()
        {
            var json = HttpContext.Session.GetString(KhoaLichSu);
            if (string.IsNullOrEmpty(json)) return new List<ChatLuot>();
            try
            {
                return JsonSerializer.Deserialize<List<ChatLuot>>(json) ?? new List<ChatLuot>();
            }
            catch (JsonException)
            {
                return new List<ChatLuot>();
            }
        }

        private void LuuLichSu(List<ChatLuot> lichSu)
        {
            if (lichSu.Count > SoLuotNguCanh)
            {
                lichSu.RemoveRange(0, lichSu.Count - SoLuotNguCanh);
            }
            HttpContext.Session.SetString(KhoaLichSu, JsonSerializer.Serialize(lichSu));
        }

        // Gioi han 15 luot/phut moi phien: chan lam dung API key ton phi.
        private bool ConLuotHoi()
        {
            var now = DateTime.UtcNow;
            var batDau = now;
            var dem = 0;

            var raw = HttpContext.Session.GetString(KhoaDem);
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split('|');
                if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && int.TryParse(parts[1], out var so))
                {
                    batDau = new DateTime(ticks, DateTimeKind.Utc);
                    dem = so;
                }
            }

            if ((now - batDau).TotalSeconds >= 60)
            {
                batDau = now;
                dem = 0;
            }

            if (dem >= GioiHanMoiPhut) return false;

            HttpContext.Session.SetString(KhoaDem, $"{batDau.Ticks}|{dem + 1}");
            return true;
        }
    }
}
