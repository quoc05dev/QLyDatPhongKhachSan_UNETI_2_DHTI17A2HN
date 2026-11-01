using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLyDatPhongKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddHoatDongVaDanhGia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    MaDanhGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatPhong = table.Column<int>(type: "int", nullable: false),
                    TenKhachHang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    BinhLuan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TinhCam = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiemCamXuc = table.Column<double>(type: "float", nullable: false),
                    GoiYPhanHoi = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    PhanHoi = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    DaPhanHoi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.MaDanhGia);
                    table.ForeignKey(
                        name: "FK_DanhGia_DatPhong_MaDatPhong",
                        column: x => x.MaDatPhong,
                        principalTable: "DatPhong",
                        principalColumn: "MaDatPhong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoatDong",
                columns: table => new
                {
                    MaHoatDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    TenNguoiDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoaiHoatDong = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaLienQuan = table.Column<int>(type: "int", nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    LaThongBao = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoatDong", x => x.MaHoatDong);
                    table.ForeignKey(
                        name: "FK_HoatDong_DatPhong_MaLienQuan",
                        column: x => x.MaLienQuan,
                        principalTable: "DatPhong",
                        principalColumn: "MaDatPhong",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaDatPhong",
                table: "DanhGia",
                column: "MaDatPhong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoatDong_MaLienQuan",
                table: "HoatDong",
                column: "MaLienQuan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "HoatDong");
        }
    }
}
