using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.HealthChecks
{
    /// <summary>
    /// Health check xác nhận ứng dụng kết nối được database.
    /// Dùng cho endpoint /health (phục vụ HEALTHCHECK của Docker).
    /// </summary>
    public class DbHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _db;

        public DbHealthCheck(AppDbContext db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var ketNoiDuoc = await _db.Database.CanConnectAsync(cancellationToken);
            return ketNoiDuoc
                ? HealthCheckResult.Healthy("Kết nối database OK.")
                : HealthCheckResult.Unhealthy("Không kết nối được database.");
        }
    }
}
