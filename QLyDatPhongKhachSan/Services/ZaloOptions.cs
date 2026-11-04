namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Gui tin qua Zalo OA (tuy chon). Thieu cau hinh => khong gui, khong loi.
    /// Dien qua env/user-secrets: Zalo__AppId, Zalo__SecretKey,
    /// Zalo__RefreshToken (lay tu API Explorer cua Zalo for Developers), Zalo__UserId
    /// (user da follow OA - lay tu log webhook /Zalo/Webhook khi nhan tin cho OA).
    /// Refresh token cua Zalo chi dung 1 lan, token moi duoc luu ra TokenFile.
    /// </summary>
    public class ZaloOptions
    {
        public string AppId { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string UserId { get; set; } = "";
        public string TokenFile { get; set; } = "Data/zalo-token.json";
        public int TimeoutSeconds { get; set; } = 8;

        public bool DungDuoc =>
            !string.IsNullOrWhiteSpace(AppId)
            && !string.IsNullOrWhiteSpace(SecretKey)
            && !string.IsNullOrWhiteSpace(RefreshToken)
            && !string.IsNullOrWhiteSpace(UserId);
    }
}
