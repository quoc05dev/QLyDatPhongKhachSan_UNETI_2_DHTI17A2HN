namespace QLyDatPhongKhachSan.Services
{
    /// <summary>Mot luot hoi dap trong phien chat (dung lam history cho LLM).</summary>
    public record ChatLuot(string VaiTro, string NoiDung);

    /// <summary>
    /// Hoi dap tu dong cho khach (chatbot CSKH).
    /// LlmChatbotService goi LLM ben ngoai khi co ApiKey; khong co thi roi ve rule-based.
    /// </summary>
    public interface IChatbotService
    {
        /// <summary>True khi da cau hinh API key va bat LLM.</summary>
        bool DangDungAi { get; }

        Task<string> HoiAsync(
            string cauHoi,
            int? maTaiKhoan,
            string? vaiTro = null,
            string? hoTen = null,
            IReadOnlyList<ChatLuot>? lichSu = null);

        /// <summary>Tom tat du kien that tu database de lam ngu canh cho LLM.</summary>
        Task<string> TaoDuKienAsync(int? maTaiKhoan, string? vaiTro = null, string? hoTen = null);
    }
}
