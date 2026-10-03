// Ho va ten: Nguyen Hai Nam
// Ma sinh vien: 22103100118
// Noi dung: ViewModel chua bo loc, tim kiem va phan trang

using System;
using System.Collections.Generic;
using QLyDatPhongKhachSan.Models;

namespace QLyDatPhongKhachSan.ViewModels
{
    public class TiepNhanDatPhongViewModel
    {
        public List<DatPhong> DanhSachDatPhong { get; set; } = new List<DatPhong>();

        public string? TuKhoa { get; set; }
        public string? TrangThai { get; set; }
        public int? MaLoaiPhong { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? SapXep { get; set; }

        public int TrangHienTai { get; set; } = 1;
        public int TongSoTrang { get; set; }
        public int TongBanGhi { get; set; }
    }
}