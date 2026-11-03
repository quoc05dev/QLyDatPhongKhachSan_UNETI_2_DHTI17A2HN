using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Chatbot goi LLM ben ngoai (OpenAI-compatible). Prompt duoc bom du kien that
    /// tu database qua ChatbotService.TaoDuKienAsync; neu chua co ApiKey hoac goi loi
    /// thi tu dong roi ve rule-based de chatbot luon tra loi duoc.
    /// </summary>
    public class LlmChatbotService : IChatbotService
    {
        private readonly ChatbotService _rule;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ChatbotAiOptions _options;
        private readonly ILogger<LlmChatbotService> _logger;

        public LlmChatbotService(
            ChatbotService rule,
            IHttpClientFactory httpFactory,
            IOptions<ChatbotAiOptions> options,
            ILogger<LlmChatbotService> logger)
        {
            _rule = rule;
            _httpFactory = httpFactory;
            _options = options.Value;
            _logger = logger;
        }

        public bool DangDungAi => _options.DungDuoc;

        public async Task<string> HoiAsync(
            string cauHoi,
            int? maTaiKhoan,
            string? vaiTro = null,
            string? hoTen = null,
            IReadOnlyList<ChatLuot>? lichSu = null)
        {
            if (!DangDungAi)
            {
                return await _rule.HoiAsync(cauHoi, maTaiKhoan, vaiTro, hoTen, lichSu);
            }

            try
            {
                var duKien = await _rule.TaoDuKienAsync(maTaiKhoan, vaiTro, hoTen);
                var messages = new List<object>
                {
                    new { role = "system", content = _options.SystemPrompt + "\n\nDữ kiện hiện tại:\n" + duKien }
                };
                foreach (var luot in (lichSu ?? Array.Empty<ChatLuot>()).TakeLast(_options.MaxHistoryMessages))
                {
                    messages.Add(new { role = luot.VaiTro == "user" ? "user" : "assistant", content = luot.NoiDung });
                }
                messages.Add(new { role = "user", content = cauHoi });

                var payload = JsonSerializer.Serialize(new
                {
                    model = _options.Model,
                    messages,
                    temperature = _options.Temperature,
                    max_tokens = _options.MaxTokens
                });

                var client = _httpFactory.CreateClient("chatbot-ai");
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                // Free tier hay bi 429/5xx: thu lai 1 lan truoc khi roi ve rule-based.
                HttpResponseMessage? response = null;
                string body = "";
                for (var lan = 1; lan <= 2; lan++)
                {
                    response?.Dispose();
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        _options.BaseUrl.TrimEnd('/') + "/chat/completions")
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                    response = await client.SendAsync(request);
                    body = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode) break;
                    var ma = (int)response.StatusCode;
                    if (lan == 2 || (ma != 429 && ma < 500)) break;
                    await Task.Delay(700);
                }

                using var ketQua = response;
                if (ketQua is null || !ketQua.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"LLM tra ve {(int)(ketQua?.StatusCode ?? 0)}: {Cat(body, 300)}");
                }

                using var json = JsonDocument.Parse(body);
                var text = json.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException("LLM tra ve noi dung rong.");
                }
                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Goi LLM that bai, quay ve rule-based.");
                return await _rule.HoiAsync(cauHoi, maTaiKhoan, vaiTro, hoTen, lichSu);
            }
        }

        public Task<string> TaoDuKienAsync(int? maTaiKhoan, string? vaiTro = null, string? hoTen = null)
            => _rule.TaoDuKienAsync(maTaiKhoan, vaiTro, hoTen);

        private static string Cat(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
