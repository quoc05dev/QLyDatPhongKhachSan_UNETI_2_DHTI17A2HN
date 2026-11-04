using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Gui tin OA (consultation message). Zalo chi cho gui khi nguoi nhan da
    /// tuong tac voi OA trong 7 ngay (mien phi), nen UserId phai la le tan/admin
    /// co nhan tin cho OA dinh ky. Token: access 25h, refresh 3 thang nhung chi
    /// dung 1 lan => luu refresh moi ra TokenFile de song qua restart/deploy.
    /// </summary>
    public class ZaloNotifier : IZaloNotifier
    {
        private const string OaTokenUrl = "https://oauth.zaloapp.com/v4/oa/access_token";
        private const string OaMessageUrl = "https://openapi.zalo.me/v3.0/oa/message/cs";

        private readonly ZaloOptions _options;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ZaloNotifier> _logger;
        private readonly string _tokenFile;
        private readonly SemaphoreSlim _khoa = new(1, 1);
        private string? _accessToken;
        private DateTimeOffset _hetHan;

        public ZaloNotifier(IOptions<ZaloOptions> options, IHttpClientFactory httpFactory,
            ILogger<ZaloNotifier> logger, IWebHostEnvironment env)
        {
            _options = options.Value;
            _httpFactory = httpFactory;
            _logger = logger;
            _tokenFile = Path.IsPathRooted(_options.TokenFile)
                ? _options.TokenFile
                : Path.Combine(env.ContentRootPath, _options.TokenFile);
        }

        public bool DangBat => _options.DungDuoc;

        public async Task<bool> GuiTinAsync(string noiDung, CancellationToken cancellationToken = default)
        {
            if (!DangBat || string.IsNullOrWhiteSpace(noiDung))
            {
                return false;
            }

            try
            {
                var token = await LayAccessTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var body = JsonSerializer.Serialize(new
                {
                    recipient = new { user_id = _options.UserId },
                    message = new { text = noiDung }
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, OaMessageUrl)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.TryAddWithoutValidation("access_token", token);

                var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
                using var response = await http.SendAsync(request, cancellationToken);
                var text = await response.Content.ReadAsStringAsync(cancellationToken);

                // Zalo tra HTTP 200 kem error != 0, nen phai doc ca body.
                string? loi = null;
                if (!response.IsSuccessStatusCode || CoLoiZalo(text, out loi))
                {
                    _logger.LogWarning("Zalo gui tin that bai ({Status}): {Loi}", (int)response.StatusCode, loi ?? text);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Zalo gui tin that bai");
                return false;
            }
        }

        private async Task<string?> LayAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _hetHan)
            {
                return _accessToken;
            }

            await _khoa.WaitAsync(cancellationToken);
            try
            {
                if (_accessToken != null && DateTimeOffset.UtcNow < _hetHan)
                {
                    return _accessToken;
                }

                var daLuu = DocTokenFile();
                if (daLuu?.AccessToken != null && DateTimeOffset.UtcNow < daLuu.HetHan)
                {
                    _accessToken = daLuu.AccessToken;
                    _hetHan = daLuu.HetHan;
                    return _accessToken;
                }

                var refreshToken = string.IsNullOrWhiteSpace(daLuu?.RefreshToken)
                    ? _options.RefreshToken
                    : daLuu!.RefreshToken!;

                using var request = new HttpRequestMessage(HttpMethod.Post, OaTokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["app_id"] = _options.AppId,
                        ["grant_type"] = "refresh_token",
                        ["refresh_token"] = refreshToken
                    })
                };
                request.Headers.TryAddWithoutValidation("secret_key", _options.SecretKey);

                var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
                using var response = await http.SendAsync(request, cancellationToken);
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Zalo refresh token that bai ({Status}): {Body}", (int)response.StatusCode, text);
                    return null;
                }

                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var accessToken = LayChuoi(root, "access_token");
                var refreshMoi = LayChuoi(root, "refresh_token") ?? refreshToken;
                var seconds = LaySo(root, "expires_in");
                if (CoLoiZalo(text, out var loiToken))
                {
                    _logger.LogWarning("Zalo refresh token loi: {Loi}", loiToken);
                    return null;
                }
                if (string.IsNullOrEmpty(accessToken) || seconds <= 0)
                {
                    _logger.LogWarning("Zalo refresh token tra ve du lieu la: {Body}", text);
                    return null;
                }

                // Tru 2 phut de khong dung token sat gio het han.
                _hetHan = DateTimeOffset.UtcNow.AddSeconds(seconds - 120);
                _accessToken = accessToken;
                LuuTokenFile(accessToken, refreshMoi, _hetHan);
                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Zalo lay access token that bai");
                return null;
            }
            finally
            {
                _khoa.Release();
            }
        }

        private sealed record TokenLuu(string? AccessToken, string? RefreshToken, DateTimeOffset HetHan);

        private TokenLuu? DocTokenFile()
        {
            try
            {
                if (!File.Exists(_tokenFile))
                {
                    return null;
                }

                using var doc = JsonDocument.Parse(File.ReadAllText(_tokenFile));
                var root = doc.RootElement;
                return new TokenLuu(
                    LayChuoi(root, "access_token"),
                    LayChuoi(root, "refresh_token"),
                    DateTimeOffset.TryParse(LayChuoi(root, "het_han"), out var hetHan) ? hetHan : DateTimeOffset.MinValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Zalo doc token file that bai: {File}", _tokenFile);
                return null;
            }
        }

        private void LuuTokenFile(string accessToken, string refreshToken, DateTimeOffset hetHan)
        {
            try
            {
                var dir = Path.GetDirectoryName(_tokenFile);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken,
                    het_han = hetHan.ToString("O")
                });
                File.WriteAllText(_tokenFile, json);
            }
            catch (Exception ex)
            {
                // Khong chan gui tin; chi mat refresh token moi khi restart.
                _logger.LogWarning(ex, "Zalo luu token file that bai: {File}", _tokenFile);
            }
        }

        private static bool CoLoiZalo(string body, out string? loi)
        {
            loi = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    var code = err.ValueKind == JsonValueKind.Number
                        ? err.GetInt32()
                        : int.TryParse(err.ToString(), out var parsed) ? parsed : 0;
                    if (code != 0)
                    {
                        loi = doc.RootElement.TryGetProperty("message", out var msg)
                            ? $"{code}: {msg}"
                            : code.ToString();
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Body khong phai JSON => coi nhu khong co ma loi.
            }

            return false;
        }

        private static string? LayChuoi(JsonElement root, string ten)
        {
            if (!root.TryGetProperty(ten, out var value))
            {
                return null;
            }

            var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static int LaySo(JsonElement root, string ten)
        {
            var text = LayChuoi(root, ten);
            return int.TryParse(text, out var so) ? so : 0;
        }
    }
}
