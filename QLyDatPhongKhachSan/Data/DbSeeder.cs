using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace QLyDatPhongKhachSan.Data
{
    /// <summary>
    /// Nạp dữ liệu mẫu từ file SQL dạng sqlcmd (có tách batch bằng "GO").
    /// Dùng cho môi trường deploy/demo để có sẵn tài khoản test và dữ liệu thống kê.
    /// </summary>
    public static class DbSeeder
    {
        // sqlcmd hiểu "GO" để tách batch, còn SqlCommand thì không.
        // Vì vậy phải tự tách và gửi từng batch trên cùng một kết nối
        // (script có BEGIN TRANSACTION ... COMMIT trải qua nhiều batch).
        private static readonly Regex MauGo = new(
            pattern: @"^[ \t]*GO[ \t]*(?:--.*)?$",
            options: RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Chay(AppDbContext db, string duongDanFileSql, ILogger logger)
        {
            var noiDung = File.ReadAllText(duongDanFileSql);
            var danhSachBatch = MauGo.Split(noiDung)
                                     .Where(b => !string.IsNullOrWhiteSpace(b))
                                     .ToList();

            var ketNoi = (SqlConnection)db.Database.GetDbConnection();
            var canMoKetNoi = ketNoi.State != ConnectionState.Open;
            if (canMoKetNoi)
            {
                ketNoi.Open();
            }

            try
            {
                foreach (var batch in danhSachBatch)
                {
                    using var lenh = ketNoi.CreateCommand();
                    lenh.CommandText = batch;
                    lenh.CommandTimeout = 120;
                    lenh.ExecuteNonQuery();
                }
            }
            finally
            {
                if (canMoKetNoi)
                {
                    ketNoi.Close();
                }
            }

            logger.LogInformation("Đã thực thi {SoBatch} batch dữ liệu mẫu.", danhSachBatch.Count);
        }
    }
}
