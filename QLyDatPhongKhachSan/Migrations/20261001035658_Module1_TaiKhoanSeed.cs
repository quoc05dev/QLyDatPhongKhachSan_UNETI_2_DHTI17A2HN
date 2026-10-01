using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QLyDatPhongKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class Module1_TaiKhoanSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TaiKhoan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "TaiKhoan",
                columns: new[] { "MaTaiKhoan", "Email", "HoTen", "MatKhau", "NgayTao", "SoDienThoai", "TenDangNhap", "TrangThai", "VaiTro" },
                values: new object[,]
                {
                    { 1, "admin@hotel.com", "Quản Trị Viên", "123456", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0901234567", "admin", true, "Admin" },
                    { 2, "nhanvien@hotel.com", "Nhân Viên Lễ Tân", "123456", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0902345678", "nhanvien", true, "NhanVien" },
                    { 3, "khachhang@gmail.com", "Nguyễn Văn Khách", "123456", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0903456789", "khachhang", true, "KhachHang" },
                    { 4, "khoa@gmail.com", "Tài Khoản Bị Khóa", "123456", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0904567890", "khoa", false, "KhachHang" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TaiKhoan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
