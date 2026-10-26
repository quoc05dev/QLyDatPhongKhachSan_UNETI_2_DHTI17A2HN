# BÁO CÁO NHIỆM VỤ - QLyDatPhongKhachSan

Thời gian: 2026-10-24

## 1) Refactor Repository Pattern (Module 5 - SV5: Đặng Minh Quốc 23103100069)
- Tạo interface `Repositories/Interfaces/` (IChiTietDatPhongRepository, IDatPhongRepository, IDichVuRepository, IKhachHangRepository, ILoaiPhongRepository, IPhongRepository, ITaiKhoanRepository) và triển khai tương ứng `Repositories/`.
- Thêm `IGenericRepository<T>` + `GenericRepository<T>`; đăng ký DI trong `Program.cs`: `AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))` + 7 repo chuyên biệt.
- Chuyển toàn bộ 11 Controller (ChiTietDatPhong, DangNhap, DatPhong, DichVu, Home, KhachHang, LoaiPhong, Phong, QuanLyDatPhong, TaiKhoan, ThongKe) dùng repository thay `AppDbContext/_context`.
- Build Debug/Release: 0 lỗi. Commit `3ea0760`, merge vào `main` qua `da1b4f2`.

## 2) Sửa bug trang "Tiếp nhận & quản lý trạng thái" (QuanLyDatPhong)
- `Views/QuanLyDatPhong/Index.cshtml`: đổi link phân trang `asp-route-page` -> `asp-route-trang` (action dùng tham số `trang`).
- `Controllers/QuanLyDatPhongController.cs`: `Index` nhận `int? trang`, `ChiTiet` truyền `ViewBag.PhongDoiKhaDung` đúng.
- `Views/QuanLyDatPhong/ChiTiet.cshtml`: đổi dùng `ViewBag.PhongDoiKhaDung`, xóa thẻ `</article>` thừa.

## 3) Sửa bug "khách hàng nhập thông tin khác vẫn hiển thị cũ"
- Nguyên nhân: FormTagHelper tự động sinh token antiforgery cho `<form asp-action method="post">`. Có 15 chỗ dùng `@Html.AntiForgeryToken()` thủ công gây **2 token** → `StringValues` nối bằng `,` → xác thực thất bại **HTTP 400**, POST không chạy.
- Đã xóa toàn bộ `@Html.AntiForgeryToken()` thừa trong 10 view (QuanLyDatPhong/ChiTiet, DatPhong/Book, DatPhong/Details, DatPhong/LichSu, KhachHang/Create/Edit/Delete/Index/Profile, DangNhap/DoiMatKhau).
- `Controllers/DatPhongController.cs`: đồng bộ lưu `HoTenKhachHang/SoDienThoai/CCCD/Email` vào `KhachHang`, cập nhật `TaiKhoan` liên quan, lưu session `HoTen` khi đặt phòng.
- Kiểm tra thực tế (LocalDB + HTTP): admin chuyển trạng thái thành công; cập nhật hồ sơ khách hàng lưu và hiển thị ngay.

## 4) Sửa lỗi đè chồng responsive (trang chủ)
- `wwwroot/css/design.css`:
  - `.booking-shell`: bỏ `height:0`, dùng `margin-top: -57px` (mobile `-40px`) thay `transform: translateY(-50%)`.
  - `.booking-panel`: bỏ `transform: translateY(-50%)/(-40px)`.
  - Xóa height cố định ở media queries.
- Đo bằng Chrome headless 390–1440px: không còn đè lên đoạn mô tả/nút, không tràn xuống section phòng.

## Trạng thái git
- `feature/23103100069-test-repository-pattern`: `b34949d` (pushed)
- `main`: `da1b4f2` merge + `22bc99d` fix responsive (pushed lên `origin/main`)
- Build: 0 warning / 0 error
