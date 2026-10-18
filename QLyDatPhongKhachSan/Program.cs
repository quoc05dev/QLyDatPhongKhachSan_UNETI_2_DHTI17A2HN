using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Đăng ký MVC Controllers with Views
builder.Services.AddControllersWithViews();

// 3b. Health check (dùng cho endpoint /health phục vụ HEALTHCHECK của Docker)
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

var app = builder.Build();

// 4. Tạo/cập nhật schema database và (tuỳ chọn) nạp dữ liệu mẫu.
//    Trong Docker, SQL Server cần vài giây để sẵn sàng nên thao tác này có thử lại.
KhoiTaoDatabase(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS redirect: nginx terminate SSL va truyen X-Forwarded-Proto: https vao container.
// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true (khai bao trong docker-compose.yml) tu dong
// kich hoat ForwardedHeaders middleware truoc pipeline, nen UseHttpsRedirection se doc
// dung scheme "https" tu header va khong gay redirect loop.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Endpoint cho HEALTHCHECK: 200 khi app + database san sang, 503 khi khong.
app.MapHealthChecks("/health");

app.Run();

// Áp dụng EF Core Migration để tạo/cập nhật database, sau đó nạp dữ liệu mẫu
// khi bật cấu hình "SeedSampleData" (dùng cho môi trường deploy/demo).
static void KhoiTaoDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("KhoiTaoDatabase");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int soLanThu = 10;
    for (var lan = 1; ; lan++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Đã áp dụng Migration cho database.");
            break;
        }
        catch (Exception ex) when (lan < soLanThu)
        {
            logger.LogWarning("Chưa kết nối được database (lần {Lan}/{Tong}): {Message}", lan, soLanThu, ex.Message);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    // SeedSampleData mặc định false (ẩn) => chạy local trên LocalDB không bị ghi đè dữ liệu.
    if (!app.Configuration.GetValue<bool>("SeedSampleData"))
    {
        return;
    }

    if (db.TaiKhoans.Any())
    {
        logger.LogInformation("Database đã có dữ liệu, bỏ qua nạp dữ liệu mẫu.");
        return;
    }

    var duongDan = Path.Combine(app.Environment.ContentRootPath, "Database", "01-du-lieu-mau.sql");
    if (File.Exists(duongDan))
    {
        DbSeeder.Chay(db, duongDan, logger);
        logger.LogInformation("Đã nạp dữ liệu mẫu từ {DuongDan}.", duongDan);
    }
    else
    {
        logger.LogWarning("Bật SeedSampleData nhưng không tìm thấy file {DuongDan}.", duongDan);
    }
}
