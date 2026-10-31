// Họ và tên: Đặng Minh Quốc
// Mã sinh viên: 23103100069
// Nội dung: Chatbot hỏi đáp CSKH (rule-based, dữ liệu thật từ database).

using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Services;

namespace QLyDatPhongKhachSan.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // POST /Chatbot/Hoi — nhận câu hỏi dạng JSON, trả về câu trả lời.
        public record ChatbotHoiRequest(string CauHoi);

        [HttpPost]
        public async Task<IActionResult> Hoi([FromBody] ChatbotHoiRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.CauHoi))
            {
                return BadRequest(new { cauTraLoi = "Bạn chưa nhập câu hỏi." });
            }

            // Câu hỏi dài bất thường thì cắt ngắn (chống spam payload).
            var cauHoi = request.CauHoi.Length > 300 ? request.CauHoi[..300] : request.CauHoi;
            var cauTraLoi = await _chatbotService.HoiAsync(cauHoi, HttpContext.Session.GetMaTaiKhoan());

            return Ok(new { cauTraLoi });
        }
    }
}
