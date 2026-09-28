# 📋 KẾ HOẠCH CHI TIẾT — ĐỀ TÀI 15

## XÂY DỰNG HỆ THỐNG QUẢN LÝ ĐẶT PHÒNG KHÁCH SẠN

**Môn:** Thực hành Lập trình .NET | **Nhóm:** 2 | **Lớp:** DHTI17A2HN  
**Công nghệ:** ASP.NET Core 10 MVC + EF Core 10 + SQL Server

---

## 1. 🎯 Tổng Quan

Xây dựng ứng dụng Web quản lý đặt phòng khách sạn, hỗ trợ quy trình nghiệp vụ từ quản lý dữ liệu nền, tiếp nhận giao dịch, xử lý trạng thái đến Dashboard và thống kê.

### 3 Vai Trò

| Vai trò | Quyền hạn |
|---------|-----------|
| 🔴 **Admin** | Quản lý tài khoản, loại phòng, phòng và dữ liệu dùng chung |
| 🟡 **Lễ tân** | Quản lý phòng, khách hàng, đặt phòng, nhận/trả phòng, Dashboard, thống kê |
| 🟢 **Khách hàng** | Đăng nhập, quản lý thông tin cá nhân, tìm & đặt phòng, theo dõi |

### Luồng Nghiệp Vụ

```
Đăng nhập → Tìm phòng → Kiểm tra ngày → Đặt phòng → Xác nhận → Nhận phòng → Trả phòng → Tính tiền
```

### Luồng Trạng Thái

```
Chờ xử lý → Đang xử lý → Hoàn thành
     ↓              ↓
   Hủy/Từ chối (khi thỏa điều kiện)
```

---

## 2. 🛠 Công Nghệ

| Thành phần | Yêu cầu |
|------------|---------|
| SDK | .NET 10 |
| Framework | ASP.NET Core 10 MVC |
| Ngôn ngữ | C# |
| ORM | Entity Framework Core 10 (Code First) |
| Database | SQL Server |
| Query | LINQ |
| View | Razor View |
| Frontend | HTML/CSS/JavaScript |
| IDE | Visual Studio 2022 / VS Code |
| VCS | Git/GitHub |

> ⛔ **KHÔNG** dùng ASP.NET MVC trên .NET Framework | **KHÔNG** dùng ASP.NET Core < 10 | Package EF Core phải version 10

---

## 3. 🗄️ Thiết Kế CSDL

### Các Entity & Quan Hệ

```
TaiKhoan (PK: MaTaiKhoan)
├── TenDangNhap, MatKhau, HoTen, Email, VaiTro, TrangThai
└──→ KhachHang (1-1)

LoaiPhong (PK: MaLoaiPhong)
├── TenLoai, SoNguoiToiDa, DonGiaNgay, MoTa, TrangThai
└──→ Phong (1-N)

Phong (PK: MaPhong | FK: MaLoaiPhong)
├── SoPhong, Tang, HuongPhong, TienNghi, DonGia, TrangThai, GhiChu, NgayBaoTri
└──→ ChiTietDatPhong (1-N)

KhachHang (PK: MaKhachHang | FK: MaTaiKhoan)
├── HoTen, NgaySinh, GioiTinh, CCCD, SoDienThoai, Email, DiaChi, QuocTich, TrangThai
└──→ DatPhong (1-N)

DatPhong (PK: MaDatPhong | FK: MaKhachHang)
├── NgayDat, NgayNhan, NgayTraDuKien, SoNguoi, TienCoc, TrangThai, GhiChu
└──→ ChiTietDatPhong (1-N)

ChiTietDatPhong (PK: MaChiTiet | FK: MaDatPhong, MaPhong)
├── NgayNhan, NgayTra, DonGia, SoNgay, ThanhTien, TrangThai

DichVu (PK: MaDichVu)
├── TenDichVu, Gia, MoTa, TrangThai
```

---

## 4. 👥 Phân Công 05 Module

---

### 📦 Module 1 — SV1: Tài Khoản, Đăng Nhập, Phân Quyền & Loại Phòng

**Entity:** `TaiKhoan`, `LoaiPhong`

| STT | Chức năng | Mô tả | Ưu tiên |
|-----|-----------|-------|---------|
| 1.1 | Entity TaiKhoan | MaTaiKhoan, TenDangNhap, MatKhau, HoTen, Email, VaiTro, TrangThai + Data Annotation | 🔴 Cao |
| 1.2 | Entity LoaiPhong | MaLoaiPhong, TenLoai, SoNguoiToiDa, DonGiaNgay, MoTa, TrangThai + Data Annotation | 🔴 Cao |
| 1.3 | Đăng nhập | Form đăng nhập, EF Core + LINQ, lưu Session (MaTaiKhoan, HoTen, VaiTro) | 🔴 Cao |
| 1.4 | Đăng xuất | Xóa Session, redirect trang chủ | 🔴 Cao |
| 1.5 | Phân quyền | Kiểm tra quyền tại Controller (không chỉ ẩn menu) | 🔴 Cao |
| 1.6 | CRUD TaiKhoan | Admin: danh sách, thêm, sửa, khóa/mở khóa | 🔴 Cao |
| 1.7 | CRUD LoaiPhong | Admin: danh sách, chi tiết, thêm, sửa, xóa | 🔴 Cao |
| 1.8 | Validation TaiKhoan | TenDangNhap không trùng, MatKhau bắt buộc, Email format | 🔴 Cao |
| 1.9 | Validation LoaiPhong | TenLoai không trùng, kiểm tra tham chiếu khi xóa | 🟡 TB |
| 1.10 | Thông báo lỗi | Validation message trên View | 🟡 TB |

**Files:** `Models/TaiKhoan.cs` · `Models/LoaiPhong.cs` · `Controllers/TaiKhoanController.cs` · `Controllers/LoaiPhongController.cs` · `Views/TaiKhoan/` · `Views/LoaiPhong/` · `Views/DangNhap/` · `Filters/AuthorizationFilter.cs`

---

### 📦 Module 2 — SV2: Quản Lý Phòng, Tìm Kiếm, Lọc, Sắp Xếp, Phân Trang

**Entity:** `Phong`

| STT | Chức năng | Mô tả | Ưu tiên |
|-----|-----------|-------|---------|
| 2.1 | Entity Phong | MaPhong, SoPhong, MaLoaiPhong (FK), Tang, HuongPhong, TienNghi, DonGia, TrangThai, GhiChu, NgayBaoTri | 🔴 Cao |
| 2.2 | CRUD Phong | Admin/NV: danh sách, chi tiết, thêm, sửa, xóa | 🔴 Cao |
| 2.3 | Tìm kiếm | Số phòng, Tên loại phòng, Tên khách hàng | 🔴 Cao |
| 2.4 | Lọc | Loại phòng, Tầng, Trạng thái, Khoảng giá, Phòng trống/khoảng ngày. Kết hợp được | 🔴 Cao |
| 2.5 | Sắp xếp | Số phòng ↑↓, Đơn giá ↑↓, Tầng ↑↓, Tên loại A→Z | 🔴 Cao |
| 2.6 | Phân trang | Skip()/Take() trên LINQ, trang trước/sau, số trang | 🔴 Cao |
| 2.7 | Kết hợp S+F+S+P | Giữ nguyên điều kiện khi chuyển trang | 🔴 Cao |
| 2.8 | Validation | Data Annotation, giá trị ≥ 0, không xóa phòng có lịch sử | 🟡 TB |
| 2.9 | View khách hàng | Hiển thị khả dụng/không, khóa ngoại dùng select | 🟡 TB |
| 2.10 | ViewModel | Kết hợp dữ liệu phòng + điều kiện search/filter/sort | 🟡 TB |

**Files:** `Models/Phong.cs` · `ViewModels/PhongViewModel.cs` · `Controllers/PhongController.cs` · `Views/Phong/` · `Views/Shared/_PaginationPartial.cshtml`

---

### 📦 Module 3 — SV3: Khách Hàng, Hồ Sơ, Đặt Phòng, Theo Dõi

**Entity:** `KhachHang`, `DatPhong`

| STT | Chức năng | Mô tả | Ưu tiên |
|-----|-----------|-------|---------|
| 3.1 | Entity KhachHang | MaKhachHang, MaTaiKhoan (FK), HoTen, NgaySinh, GioiTinh, CCCD, SoDienThoai, Email, DiaChi, QuocTich, TrangThai | 🔴 Cao |
| 3.2 | Entity DatPhong | MaDatPhong, MaKhachHang (FK), NgayDat, NgayNhan, NgayTraDuKien, SoNguoi, TienCoc, TrangThai | 🔴 Cao |
| 3.3 | Hồ sơ cá nhân | Khách hàng chỉ xem/sửa hồ sơ của chính mình | 🔴 Cao |
| 3.4 | Tạo đặt phòng | Kiểm tra: user tồn tại, phòng đủ điều kiện, thời gian hợp lệ, không trùng | 🔴 Cao |
| 3.5 | Hủy đặt phòng | Chỉ hủy ở trạng thái cho phép | 🔴 Cao |
| 3.6 | Theo dõi | Danh sách giao dịch, trạng thái hiện tại, kết quả | 🔴 Cao |
| 3.7 | Bảo mật URL | Thay đổi mã trên URL không xem được dữ liệu người khác | 🔴 Cao |
| 3.8 | Validation KhachHang | HoTen bắt buộc, NgaySinh, Email, SoDienThoai | 🟡 TB |
| 3.9 | Validation DatPhong | NgayNhan < NgayTraDuKien, SoNguoi > 0, TienCoc ≥ 0 | 🟡 TB |
| 3.10 | CRUD KhachHang | Admin/NV quản lý danh sách khách hàng | 🟡 TB |

**Files:** `Models/KhachHang.cs` · `Models/DatPhong.cs` · `ViewModels/DatPhongViewModel.cs` · `Controllers/KhachHangController.cs` · `Controllers/DatPhongController.cs` · `Views/KhachHang/` · `Views/DatPhong/`

---

### 📦 Module 4 — SV4: Tiếp Nhận, Nhận Phòng, Trả Phòng, Quản Lý Trạng Thái

**Xử lý trên:** `DatPhong`, `Phong`

| STT | Chức năng | Mô tả | Ưu tiên |
|-----|-----------|-------|---------|
| 4.1 | Danh sách cần xử lý | Tìm theo khách hàng/phòng; lọc theo loại, trạng thái, khoảng ngày | 🔴 Cao |
| 4.2 | Tiếp nhận | NV xem thông tin → Chờ xử lý → Đang xử lý | 🔴 Cao |
| 4.3 | Nhận phòng | Đang xử lý → Đã nhận phòng | 🔴 Cao |
| 4.4 | Trả phòng | Đã nhận → Hoàn thành, cập nhật phòng về trống | 🔴 Cao |
| 4.5 | Kiểm tra trùng thời gian | LINQ kiểm tra không trùng khoảng thời gian | 🔴 Cao |
| 4.6 | Kiểm tra sức chứa | SoNguoi ≤ SoNguoiToiDa của LoaiPhong | 🔴 Cao |
| 4.7 | Trạng thái phòng | Chỉ đặt phòng đang kinh doanh | 🔴 Cao |
| 4.8 | Luồng trạng thái | Chờ → Đang xử lý → Hoàn thành; Hủy/Từ chối theo điều kiện | 🔴 Cao |
| 4.9 | Kiểm tra ngày | NgayTra > NgayNhan | 🟡 TB |
| 4.10 | Kiểm soát số lượng | LINQ đếm/kiểm tra, cảnh báo khi đạt giới hạn | 🟡 TB |

**Files:** `Controllers/TiepNhanController.cs` · `ViewModels/TiepNhanViewModel.cs` · `Views/TiepNhan/`

---

### 📦 Module 5 — SV5: Tính Tiền, Dashboard, Thống Kê, Báo Cáo

**Entity:** `ChiTietDatPhong`, `DichVu`

| STT | Chức năng | Mô tả | Ưu tiên |
|-----|-----------|-------|---------|
| 5.1 | Entity ChiTietDatPhong | MaChiTiet, MaDatPhong (FK), MaPhong (FK), NgayNhan, NgayTra, DonGia, SoNgay, ThanhTien, TrangThai | 🔴 Cao |
| 5.2 | Entity DichVu | MaDichVu, TenDichVu, Gia, MoTa, TrangThai | 🔴 Cao |
| 5.3 | Tính tiền | SoNgay × DonGia = ThanhTien | 🔴 Cao |
| 5.4 | Cập nhật kết quả | Chỉ giao dịch đủ điều kiện mới cập nhật | 🔴 Cao |
| 5.5 | Dashboard | Tổng loại phòng, phòng, phòng khả dụng, khách hàng, giao dịch theo trạng thái, doanh thu | 🔴 Cao |
| 5.6 | Số phòng theo loại | LINQ GroupBy() | 🔴 Cao |
| 5.7 | Tỷ lệ sử dụng phòng | LINQ Count()/tổng | 🔴 Cao |
| 5.8 | Lượt đặt theo tháng | LINQ GroupBy() theo tháng | 🔴 Cao |
| 5.9 | Phòng đặt nhiều nhất | LINQ OrderByDescending() | 🟡 TB |
| 5.10 | Doanh thu theo tháng/loại | LINQ Sum(), GroupBy() | 🔴 Cao |
| 5.11 | Số lượt hủy | LINQ Count() | 🟡 TB |

**Files:** `Models/ChiTietDatPhong.cs` · `Models/DichVu.cs` · `ViewModels/DashboardViewModel.cs` · `ViewModels/ThongKeViewModel.cs` · `Controllers/ChiTietDatPhongController.cs` · `Controllers/DichVuController.cs` · `Controllers/ThongKeController.cs` · `Views/ThongKe/` · `Views/ChiTietDatPhong/`

---

## 5. 📁 Cấu Trúc Thư Mục

```
QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN/
├── README.md
├── .gitignore
├── QLyDatPhongKhachSan.sln
└── QLyDatPhongKhachSan/
    ├── Models/
    │   ├── TaiKhoan.cs          (SV1)
    │   ├── LoaiPhong.cs         (SV1)
    │   ├── Phong.cs             (SV2)
    │   ├── KhachHang.cs         (SV3)
    │   ├── DatPhong.cs          (SV3)
    │   ├── ChiTietDatPhong.cs   (SV5)
    │   └── DichVu.cs            (SV5)
    ├── ViewModels/
    │   ├── PhongViewModel.cs    (SV2)
    │   ├── DatPhongViewModel.cs (SV3)
    │   ├── TiepNhanViewModel.cs (SV4)
    │   ├── DashboardViewModel.cs(SV5)
    │   └── ThongKeViewModel.cs  (SV5)
    ├── Data/
    │   └── AppDbContext.cs      (Cả nhóm)
    ├── Controllers/             (Theo module phân công)
    ├── Views/                   (Theo module phân công)
    ├── Migrations/
    ├── wwwroot/ (css, js, images)
    ├── Program.cs
    └── appsettings.json
```

---

## 6. 📅 Timeline (21 ngày)

### Phase 1: Khởi Tạo (Ngày 1-3) 🏁

| Công việc | Người |
|-----------|-------|
| Thiết kế CSDL, thống nhất Entity & quan hệ | Cả nhóm |
| Tạo Project + Repository GitHub + .gitignore | SV1 |
| Tạo tất cả Entity Models + AppDbContext + DbSet | Cả nhóm |
| Migration đầu tiên + Update DB | SV1 |
| Setup Layout chung | SV2 |

### Phase 2: Phát Triển (Ngày 4-14) 💻

| Module | Tuần 1 (Ngày 4-8) | Tuần 2 (Ngày 9-14) |
|--------|-------------------|---------------------|
| SV1 | Entity + Đăng nhập + Phân quyền | CRUD TaiKhoan + LoaiPhong |
| SV2 | Entity Phong + CRUD | Search + Filter + Sort + Pagination |
| SV3 | Entity KhachHang/DatPhong + Hồ sơ | Đặt phòng + Hủy + Theo dõi |
| SV4 | Tiếp nhận + Nhận phòng | Trả phòng + Trạng thái + Nghiệp vụ |
| SV5 | Entity ChiTietDatPhong/DichVu + Tính tiền | Dashboard + Thống kê LINQ |

### Phase 3: Tích Hợp (Ngày 15-18) 🧪

| Công việc | Người |
|-----------|-------|
| Pull, merge & resolve conflicts | Cả nhóm |
| Test đăng nhập/phân quyền | SV1 |
| Test CRUD + Validation | Cả nhóm |
| Test luồng đặt phòng end-to-end | SV3 + SV4 |
| Test Dashboard + đối chiếu LINQ | SV5 |
| Test bảo mật URL | SV3 |

### Phase 4: Hoàn Thiện (Ngày 19-21) 📦

| Công việc | Người |
|-----------|-------|
| Dữ liệu mẫu + tài khoản kiểm thử | Cả nhóm |
| Báo cáo + Video minh chứng | Cả nhóm |
| Review code + comment người thực hiện | Cả nhóm |
| Final commit & push | Cả nhóm |

---

## 7. 📊 Dữ Liệu Mẫu

| Loại | Số lượng |
|------|----------|
| Loại phòng | 05 |
| Phòng (nhiều trạng thái) | 15+ |
| Khách hàng | 30+ |
| Tài khoản Admin | 1-2 |
| Tài khoản Nhân viên | 2-3 |
| Giao dịch đặt phòng | 40-50 |
| Chi tiết đặt phòng | 15-20 |

---

## 8. 📝 Quy Định Commit

### Format

```
[Mã SV] [Module] Nội dung công việc (KHÔNG DẤU)
```

### Ví dụ

```
[22103100001] [TaiKhoan] Tao Entity TaiKhoan va Data Annotation
[22103100002] [Phong] Them chuc nang phan trang voi Skip Take
[22103100003] [DatPhong] Kiem tra trung lich dat phong bang LINQ
[22103100004] [TiepNhan] Xu ly trang thai nhan phong va tra phong
[22103100005] [ThongKe] Them Dashboard tong quan voi LINQ
```

### Comment Header Mã Nguồn

```csharp
// Họ và tên: Nguyễn Văn A
// Mã sinh viên: 22103100001
// Nội dung thực hiện: Quản lý tài khoản, đăng nhập,
// phân quyền và quản lý loại phòng.
```

> Áp dụng cho: Model, Controller, ViewModel, View, các lớp xử lý khác.

---

## 9. ⭐ Chức Năng Nâng Cao (Không bắt buộc)

> ⚠️ **KHÔNG** được thay thế chức năng bắt buộc còn thiếu!

| Chức năng | Mô tả |
|-----------|-------|
| Upload file/hình ảnh | Ảnh phòng, avatar khách hàng |
| Gửi email thông báo | Xác nhận đặt phòng qua email |
| Biểu đồ Chart.js | Trực quan hóa thống kê |
| AJAX | Tải dữ liệu không reload trang |
| Xuất Excel/PDF | Xuất báo cáo |
| Tìm kiếm AJAX | Tìm kiếm không tải lại trang |
| Mã hóa mật khẩu | Hash password |
| ASP.NET Core Identity | Xác thực nâng cao |

---

*© 2026 — Nhóm 2 — DHTI17A2HN — UNETI*
