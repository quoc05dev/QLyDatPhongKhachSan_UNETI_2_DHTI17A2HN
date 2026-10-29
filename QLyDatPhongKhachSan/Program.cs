using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.HealthChecks;
using QLyDatPhongKhachSan.Repositories;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1b. Đăng ký Repository generic (Controller không truy cập DbContext trực tiếp)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// 1c. Đăng ký Repository chuyên biệt theo từng entity
builder.Services.AddScoped<IDichVuRepository, DichVuRepository>();
builder.Services.AddScoped<ITaiKhoanRepository, TaiKhoanRepository>();
builder.Services.AddScoped<IKhachHangRepository, KhachHangRepository>();
builder.Services.AddScoped<ILoaiPhongRepository, LoaiPhongRepository>();
builder.Services.AddScoped<IPhongRepository, PhongRepository>();
builder.Services.AddScoped<IChiTietDatPhongRepository, ChiTietDatPhongRepository>();
builder.Services.AddScoped<IDatPhongRepository, DatPhongRepository>();

// 1d. Đăng ký dịch vụ
builder.Services.AddScoped<IEmailService, EmailService>();

// 1e. Giới hạn tần suất đăng nhập / đăng ký / đổi mật khẩu theo IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// 2. Cấu hình Session
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 2b. Xác thực ngoài (Google OAuth) để đăng nhập / đăng ký bằng tài khoản Google.
//     Chỉ bật khi đã có ClientId + ClientSecret trong appsettings hoặc biến môi trường
//     Authentication__Google__ClientId / Authentication__Google__ClientSecret.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/DangNhap/Login";
        options.AccessDeniedPath = "/DangNhap/AccessDenied";
        options.Cookie.Name = "QLyDatPhongKhachSan.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
    });

if (googleEnabled)
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/dang-nhap/google-callback";
        options.SaveTokens = false;
        options.UsePkce = true;
        // Google trả các claim này trong userinfo; map tường minh để callback kiểm tra.
        options.ClaimActions.MapJsonKey("sub", "sub");
        options.ClaimActions.MapJsonKey("email_verified", "email_verified");
    });
}

// 3. Đăng ký MVC Controllers with Views
builder.Services.AddControllersWithViews();

// 3b. Health check (dùng cho endpoint /health phục vụ HEALTHCHECK của Docker)
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

var app = builder.Build();

// 1e. Chỉ tin ForwardedHeaders từ reverse proxy tin cậy.
//     Docker: nguồn kết nối có thể là bridge gateway chứ không phải loopback,
//     nên compose bật TrustAllForwardedHeaders và web chỉ bind 127.0.0.1.
//     Kubernetes: ingress-nginx nằm ở pod IP khác nên cũng bật cờ này.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeadersOptions.KnownProxies.Add(IPAddress.Loopback);
forwardedHeadersOptions.KnownProxies.Add(IPAddress.IPv6Loopback);

if (app.Configuration.GetValue<bool>("TrustAllForwardedHeaders"))
{
    forwardedHeadersOptions.ForwardLimit = null;
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}

// 4. Tạo/cập nhật schema database và (tuỳ chọn) nạp dữ liệu mẫu.
//    Trong Docker, SQL Server cần vài giây để sẵn sàng nên thao tác này có thử lại.
KhoiTaoDatabase(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(forwardedHeadersOptions);

// Security headers cơ bản. CSP chỉ đặt các directive không phá inline script hiện có;
// khi có thời gian nên chuyển sang CSP nonce/hash đầy đủ.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] = "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'";
    await next();
});

// HTTPS redirect: nginx terminate SSL va truyen X-Forwarded-Proto: https vao container.
// ForwardedHeadersOptions (mục 1e) chỉ xử lý header từ proxy đã cấu hình.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
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

    var seedSampleData = app.Configuration.GetValue<bool>("SeedSampleData");
    var seedAdminPassword = app.Configuration["SeedAdminPassword"];

    // Phát hiện tài khoản admin còn dùng mật khẩu mặc định công khai (123456).
    // Nếu có SeedAdminPassword thì tự động thay; nếu không thì cảnh báo để admin đổi tay.
    var adminMacDinh = db.TaiKhoans.FirstOrDefault(t => t.TenDangNhap == "admin");
    if (adminMacDinh != null && MatKhauHelper.Verify("123456", adminMacDinh.MatKhau))
    {
        if (!string.IsNullOrWhiteSpace(seedAdminPassword))
        {
            adminMacDinh.MatKhau = MatKhauHelper.Hash(seedAdminPassword);
            db.SaveChanges();
            logger.LogWarning("Đã thay mật khẩu admin mặc định bằng SeedAdminPassword.");
        }
        else
        {
            logger.LogWarning("Tài khoản admin vẫn dùng mật khẩu mặc định 123456. Hãy đổi mật khẩu ngay.");
        }
    }

    // SeedSampleData mặc định false (ẩn) => chạy local trên LocalDB không bị ghi đè dữ liệu.
    if (!seedSampleData)
    {
        return;
    }

    if (db.TaiKhoans.Any())
    {
        logger.LogInformation("Database đã có dữ liệu, bỏ qua nạp dữ liệu mẫu.");
        return;
    }

    // Không bao giờ seed admin/123456 ở môi trường ngoài Development nếu chưa đặt
    // SeedAdminPassword. Nhờ vậy deploy Production không vô tình mở cổng sau.
    if (string.IsNullOrWhiteSpace(seedAdminPassword) && !app.Environment.IsDevelopment())
    {
        logger.LogWarning("SeedSampleData=true nhưng thiếu SeedAdminPassword. Bỏ qua seed để không tạo tài khoản admin mặc định.");
        return;
    }

    if (!string.IsNullOrWhiteSpace(seedAdminPassword) && seedAdminPassword.Length < 8)
    {
        logger.LogWarning("SeedAdminPassword nên có ít nhất 8 ký tự.");
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
        return;
    }

    if (!string.IsNullOrWhiteSpace(seedAdminPassword))
    {
        var admin = db.TaiKhoans.FirstOrDefault(t => t.TenDangNhap == "admin");
        if (admin != null)
        {
            admin.MatKhau = MatKhauHelper.Hash(seedAdminPassword);
            db.SaveChanges();
            logger.LogInformation("Đã đặt mật khẩu admin từ SeedAdminPassword.");
        }
    }
}
