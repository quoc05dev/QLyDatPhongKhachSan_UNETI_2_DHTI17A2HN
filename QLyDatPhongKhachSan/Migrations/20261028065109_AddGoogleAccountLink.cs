using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLyDatPhongKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAccountLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                table: "TaiKhoan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LaTaiKhoanGoogle",
                table: "TaiKhoan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_GoogleSubject",
                table: "TaiKhoan",
                column: "GoogleSubject",
                unique: true,
                filter: "[GoogleSubject] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_GoogleSubject",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "LaTaiKhoanGoogle",
                table: "TaiKhoan");
        }
    }
}
