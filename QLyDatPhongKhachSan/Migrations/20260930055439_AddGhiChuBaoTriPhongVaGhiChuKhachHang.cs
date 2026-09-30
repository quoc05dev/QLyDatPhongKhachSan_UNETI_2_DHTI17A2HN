using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLyDatPhongKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddGhiChuBaoTriPhongVaGhiChuKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuBaoTri",
                table: "Phong",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "KhachHang",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChuBaoTri",
                table: "Phong");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "KhachHang");
        }
    }
}
