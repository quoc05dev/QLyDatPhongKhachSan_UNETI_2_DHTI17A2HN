using System.Globalization;
using System.Text;

namespace QLyDatPhongKhachSan.Services
{
    public record KetQuaCamXuc(string TinhCam, double Diem, string GoiYPhanHoi);

    /// <summary>
    /// Phân tích cảm xúc đánh giá khách hàng (rule-based, tiếng Việt có/không dấu).
    /// Tính điểm = số từ tích cực − số từ tiêu cực, cộng trọng số số sao,
    /// rồi soạn nháp phản hồi theo cảm xúc cho Admin/Nhân viên.
    /// Muốn nâng cấp AI thật: thay class này bằng service gọi LLM — giữ nguyên signature.
    /// </summary>
    public static class CamXucAnalyzer
    {
        private static readonly string[] TuTichCuc =
        {
            "tuyet voi", "rat tot", "hoan hao", "sach se", "dep", "thien thich", "gia re",
            "gia tot", "hai long", "thich", "yen tinh", "tien nghi", "chuyen nghiep",
            "nong tinh", "gon gang", " thoai mai", "tot", "dep mat", "sang trong",
            "nhanh chong", "cham soc", "quy moi", "tro lai", "khuyen nghi", "gia tri",
            "an toan", "rung mai", "tat ca deu tot", "khong co che", "xuat sac", " tuyet",
            "dep lam", "xinh", "gon", " Sach", "nhiet tinh", "tan tam"
        };

        private static readonly string[] TuTieuCuc =
        {
            "te", "xau", "ban", "dơ", "do", "on ao", "am thap", "nong", "chet nung",
            "cham", "hong", "loi", "phi qua cao", "dat", "khong tot", "khong hai long",
            "phan nhan", "tuc gian", "gat gao", "vo van", "bo loc", "nuoc nong",
            "khong sach", "mui hoi", "hôi", "chet dien", "mat dien", "chet nuoc",
            "khong co", "nghi o lai khong muon", "khong quay lai", "gia cao", "qua dat",
            "khong xung", "lam anh huong", "nhi", "set", "con buom", "chet", "that vong"
        };

        public static KetQuaCamXuc PhanTich(string? binhLuan, int soSao)
        {
            var t = BoDau(binhLuan ?? string.Empty).ToLowerInvariant();

            var diem = 0.0;
            foreach (var tu in TuTichCuc) if (t.Contains(tu)) diem += 1;
            foreach (var tu in TuTieuCuc) if (t.Contains(tu)) diem -= 1;

            // Số sao là tín hiệu mạnh: 4-5★ kéo điểm lên, 1-2★ kéo xuống.
            diem += (soSao - 3) * 0.8;

            var tinhCam = diem >= 0.5 ? "TichCuc" : diem <= -0.5 ? "TieuCuc" : "TrungLap";
            return new KetQuaCamXuc(tinhCam, Math.Round(diem, 2), TaoGoiY(tinhCam, soSao));
        }

        private static string TaoGoiY(string tinhCam, int soSao)
        {
            return tinhCam switch
            {
                "TichCuc" => "Kính gửi quý khách, cảm ơn quý khách đã dành thời gian đánh giá cao dịch vụ của chúng tôi. "
                           + "Rất mong được đón tiếp quý khách trong những chuyến đi sắp tới!",
                "TrungLap" => "Kính gửi quý khách, cảm ơn quý khách đã phản hồi. Chúng tôi ghi nhận ý kiến của quý khách "
                            + "và sẽ cải thiện chất lượng dịch vụ. Mong có cơ hội phục vụ quý khách tốt hơn.",
                _ => "Kính gửi quý khách, chúng tôi thành thật xin lỗi về trải nghiệm chưa tốt của quý khách trong lần lưu trú "
                   + (soSao <= 2 ? "với số sao thấp " : "")
                   + "này. Bộ phận chăm sóc khách hàng mong được liên hệ trực tiếp để lắng nghe và bồi thường thỏa đáng. "
                   + "Quý khách vui lòng để lại thông tin liên hệ nhé!"
            };
        }

        private static string BoDau(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Replace("đ", "d").Replace("Đ", "D");
        }
    }
}
