using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace QLyDatPhongKhachSan.Controllers
{
    /// <summary>
    /// Webhook Zalo OA: chi ghi log user_id cua nguoi nhan tin cho OA de cau hinh
    /// Zalo__UserId (khong co side effect nen khong chan request la).
    /// Khai bao URL tren Zalo for Developers: https://&lt;domain&gt;/Zalo/Webhook
    /// </summary>
    public class ZaloController : Controller
    {
        private readonly ILogger<ZaloController> _logger;

        public ZaloController(ILogger<ZaloController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            if (!HttpMethods.IsPost(Request.Method))
            {
                return Content("ok");
            }

            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var userId = root.TryGetProperty("sender", out var sender)
                    && sender.TryGetProperty("id", out var id) ? id.ToString() : null;
                var eventName = root.TryGetProperty("event_name", out var ev) ? ev.ToString() : null;

                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("ZALO_USER_ID={UserId} event={Event}", userId, eventName);
                }
            }
            catch (JsonException)
            {
                _logger.LogDebug("Zalo webhook body khong phai JSON");
            }

            return Content("ok");
        }
    }
}
