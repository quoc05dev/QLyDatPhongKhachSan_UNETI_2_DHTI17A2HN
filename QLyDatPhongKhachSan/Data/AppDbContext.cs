// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Cấu hình DbContext, Unique Constraint và Seed dữ liệu tài khoản mẫu.

using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<DatPhong> DatPhongs { get; set; }
        public DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public DbSet<DichVu> DichVus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. TaiKhoan: TenDangNhap Unique
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasIndex(e => e.TenDangNhap).IsUnique();
            });

            // 2. Phong: SoPhong Unique & Quan hệ LoaiPhong (1-N)
            modelBuilder.Entity<Phong>(entity =>
            {
                entity.HasIndex(e => e.SoPhong).IsUnique();

                entity.HasOne(p => p.LoaiPhong)
                      .WithMany(lp => lp.Phongs)
                      .HasForeignKey(p => p.MaLoaiPhong)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. KhachHang: Quan hệ TaiKhoan (1-1 hoặc 1-N)
            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.HasOne(kh => kh.TaiKhoan)
                      .WithOne(tk => tk.KhachHang)
                      .HasForeignKey<KhachHang>(kh => kh.MaTaiKhoan)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 4. DatPhong: Quan hệ KhachHang (1-N)
            modelBuilder.Entity<DatPhong>(entity =>
            {
                entity.HasOne(dp => dp.KhachHang)
                      .WithMany(kh => kh.DatPhongs)
                      .HasForeignKey(dp => dp.MaKhachHang)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 5. ChiTietDatPhong: Quan hệ DatPhong & Phong
            modelBuilder.Entity<ChiTietDatPhong>(entity =>
            {
                entity.HasOne(ct => ct.DatPhong)
                      .WithMany(dp => dp.ChiTietDatPhongs)
                      .HasForeignKey(ct => ct.MaDatPhong)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ct => ct.Phong)
                      .WithMany(p => p.ChiTietDatPhongs)
                      .HasForeignKey(ct => ct.MaPhong)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed Dữ Liệu Ban Đầu Cho TaiKhoan
            var fixedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<TaiKhoan>().HasData(
                new TaiKhoan
                {
                    MaTaiKhoan = 1,
                    TenDangNhap = "admin",
                    MatKhau = "123456",
                    HoTen = "Quản Trị Viên",
                    Email = "admin@hotel.com",
                    SoDienThoai = "0901234567",
                    VaiTro = "Admin",
                    TrangThai = true,
                    NgayTao = fixedDate
                },
                new TaiKhoan
                {
                    MaTaiKhoan = 2,
                    TenDangNhap = "nhanvien",
                    MatKhau = "123456",
                    HoTen = "Nhân Viên Lễ Tân",
                    Email = "nhanvien@hotel.com",
                    SoDienThoai = "0902345678",
                    VaiTro = "NhanVien",
                    TrangThai = true,
                    NgayTao = fixedDate
                },
                new TaiKhoan
                {
                    MaTaiKhoan = 3,
                    TenDangNhap = "khachhang",
                    MatKhau = "123456",
                    HoTen = "Nguyễn Văn Khách",
                    Email = "khachhang@gmail.com",
                    SoDienThoai = "0903456789",
                    VaiTro = "KhachHang",
                    TrangThai = true,
                    NgayTao = fixedDate
                },
                new TaiKhoan
                {
                    MaTaiKhoan = 4,
                    TenDangNhap = "khoa",
                    MatKhau = "123456",
                    HoTen = "Tài Khoản Bị Khóa",
                    Email = "khoa@gmail.com",
                    SoDienThoai = "0904567890",
                    VaiTro = "KhachHang",
                    TrangThai = false,
                    NgayTao = fixedDate
                }
            );
        }
    }
}
