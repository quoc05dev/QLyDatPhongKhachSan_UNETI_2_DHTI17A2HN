namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Hỏi đáp tự động cho khách (chatbot CSKH).
    /// Hiện tại là rule-based (không gọi AI bên ngoài) để demo offline vẫn chạy;
    /// sau này muốn nối LLM thì viết thêm implementation khác và đổi đăng ký ở Program.cs.
    /// </summary>
    public interface IChatbotService
    {
        Task<string> HoiAsync(string cauHoi, int? maTaiKhoan);
    }
}
