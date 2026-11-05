using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLyDatPhongKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddNgayHoanThanhBaoTri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHoanThanhBaoTri",
                table: "Phong",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayHoanThanhBaoTri",
                table: "Phong");
        }
    }
}
