namespace QLyDatPhongKhachSan.Services
{
    /// <summary>
    /// Cau hinh LLM ben ngoai (OpenAI-compatible /chat/completions).
    /// ApiKey lay tu user-secrets hoac bien moi truong ChatbotAi__ApiKey, khong commit.
    /// </summary>
    public class ChatbotAiOptions
    {
        public bool Enabled { get; set; } = true;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4o-mini";
        public string ApiKey { get; set; } = "";
        public double Temperature { get; set; } = 0.4;
        public int MaxTokens { get; set; } = 500;
        public int TimeoutSeconds { get; set; } = 25;
        public int MaxHistoryMessages { get; set; } = 10;

        public string SystemPrompt { get; set; } = DefaultSystemPrompt;

        public bool DungDuoc => Enabled && !string.IsNullOrWhiteSpace(ApiKey);

        private const string DefaultSystemPrompt =
            "Bạn là trợ lý ảo của UNETI Grand Hotel & Spa (khách sạn 4 sao tại Hà Nội). "
            + "Trả lời bằng tiếng Việt, lịch sự, ngắn gọn (tối đa 6 câu), xưng 'mình' và gọi khách là 'bạn'. "
            + "Riêng giá, tình trạng phòng, dịch vụ và chính sách của khách sạn chỉ được lấy từ phần 'Dữ kiện hiện tại'; tuyệt đối không bịa các số liệu này. "
            + "Câu hỏi ngoài dữ kiện khách sạn (du lịch, ăn uống, thời tiết, kiến thức chung...) thì vẫn trả lời bằng kiến thức chung của bạn, ngắn gọn và hữu ích. "
            + "Chỉ hướng khách xem trang Đặt phòng hoặc gọi hotline 1900 6068 khi câu hỏi cần số liệu khách sạn mà dữ kiện không có. "
            + "Khi được hỏi về đặt trùng/cùng lúc, nâng hạng phòng, bảo trì, khiếu nại, khuyến mãi hay khách quen: trả lời đúng theo mục 'Chính sách vận hành' trong Dữ kiện hiện tại; "
            + "tuyệt đối không tự nghĩ ra chương trình giảm giá, mức hoàn tiền hay ngày sửa xong. Nếu dữ kiện nói chưa có thì trả lời chưa có và mời liên hệ lễ tân/hotline. "
            + "Không tiết lộ thông tin đơn đặt phòng của người khác; chỉ tra đơn khi khách đã đăng nhập đúng tài khoản. "
            + "Trả lời dạng văn bản thuần: không markdown, không ký tự trang trí, không emoji.";
    }
}
