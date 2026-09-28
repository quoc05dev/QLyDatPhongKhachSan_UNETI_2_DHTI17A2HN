# 🏨 HỆ THỐNG QUẢN LÝ ĐẶT PHÒNG KHÁCH SẠN

> **Đề tài 15** – Bài tập lớn môn Thực hành Lập trình .NET  
> **Trường:** UNETI | **Lớp:** DHTI17A2HN | **Nhóm:** 2

## 📋 Mục Lục

- [Giới thiệu](#-giới-thiệu)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cài đặt & Chạy dự án](#-cài-đặt--chạy-dự-án)
- [Kết nối Database SQL Server](#-kết-nối-database-sql-server)
- [Phân công Module](#-phân-công-module)
- [Hướng dẫn làm việc với GitHub](#-hướng-dẫn-làm-việc-với-github)
  - [Bước 1: Cài đặt Git](#bước-1-cài-đặt-git)
  - [Bước 2: Clone dự án](#bước-2-clone-dự-án)
  - [Bước 3: Tạo nhánh (Branch)](#bước-3-tạo-nhánh-branch)
  - [Bước 4: Commit & Push](#bước-4-commit--push)
  - [Bước 5: Tạo Pull Request](#bước-5-tạo-pull-request)
  - [Bước 6: Pull code mới nhất](#bước-6-pull-code-mới-nhất)
  - [Bước 7: Xử lý Conflict](#bước-7-xử-lý-conflict)
- [Quy định Commit Message](#-quy-định-commit-message)
- [Quy trình làm việc tổng thể](#-quy-trình-làm-việc-tổng-thể)
- [Tài khoản kiểm thử](#-tài-khoản-kiểm-thử)
- [Dữ liệu mẫu](#-dữ-liệu-mẫu)
- [Ghi chú quan trọng](#-ghi-chú-quan-trọng)

---

## 📖 Giới Thiệu

Ứng dụng Web quản lý đặt phòng khách sạn xây dựng bằng **ASP.NET Core 10 MVC**, hỗ trợ quy trình nghiệp vụ từ quản lý dữ liệu nền, tiếp nhận giao dịch, xử lý trạng thái đến Dashboard và thống kê.

**3 vai trò người dùng:**
- 🔴 **Admin** – Quản lý tài khoản, loại phòng, phòng, dữ liệu dùng chung
- 🟡 **Lễ tân (Nhân viên)** – Quản lý phòng, khách hàng, đặt phòng, nhận/trả phòng, Dashboard, thống kê
- 🟢 **Khách hàng** – Đăng nhập, quản lý thông tin cá nhân, tìm & đặt phòng, theo dõi đặt phòng

---

## 🛠 Công Nghệ Sử Dụng

| Thành phần | Công nghệ |
|------------|-----------|
| Framework | ASP.NET Core 10 MVC |
| Ngôn ngữ | C# |
| ORM | Entity Framework Core 10 (Code First) |
| Database | SQL Server |
| Query | LINQ |
| View Engine | Razor View |
| IDE | Visual Studio 2022 / VS Code |
| Version Control | Git / GitHub |

---

## 🚀 Cài Đặt & Chạy Dự Án

### Yêu cầu hệ thống
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express hoặc LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/downloads)

### Các bước cài đặt

```bash
# 1. Clone dự án
git clone https://github.com/<username>/QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN.git

# 2. Di chuyển vào thư mục dự án
cd QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN

# 3. Restore packages
dotnet restore

# 4. Cập nhật Connection String trong appsettings.json
# Mỗi người chỉ cần sửa đúng đoạn "Server=..." - xem mục Kết nối Database bên dưới

# 5. Tạo Database từ Migration
dotnet tool restore
dotnet ef database update

# 6. Chạy ứng dụng
dotnet run
```

> 📝 **Lưu ý:** Nếu dùng Visual Studio, mở file `.sln` → nhấn `F5` hoặc `Ctrl+F5` để chạy.

---

## 🔌 Kết Nối Database (SQL Server)

### 1. Nơi cấu hình

Connection string nằm trong **`QLyDatPhongKhachSan/appsettings.json`** và được đọc tại `Program.cs`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QLyDatPhongKhachSanDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 2. Nguyên tắc cho cả nhóm

> ✅ **Tên database luôn giữ nguyên:** `Database=QLyDatPhongKhachSanDB`
> 🔧 **Mỗi người chỉ sửa đúng đoạn `Server=`** theo SQL Server đang có trên máy mình.

Nhờ vậy ai cũng dùng chung một tên database, chỉ khác địa chỉ instance:

| SQL Server trên máy bạn | Giá trị `Server=` |
|--------------------------|-------------------|
| LocalDB (mặc định của dự án) | `Server=(localdb)\MSSQLLocalDB` |
| SQL Server Express | `Server=.\SQLEXPRESS` |
| SQL Server bản đầy đủ (mặc định) | `Server=.` |
| SQL Server của bạn trong mạng LAN | `Server=192.168.x.x\SQLEXPRESS` |

**Ví dụ cụ thể** — bạn dùng SQL Express thì chỉ cần sửa thành:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=QLyDatPhongKhachSanDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

> ⚠️ Trong JSON phải ghi `\\` thay cho `\`, ví dụ `\\SQLEXPRESS` thay cho `\SQLEXPRESS`.

### 3. Kiểm tra SQL Server nào đang có trên máy

```powershell
# Liệt kê các service SQL Server
Get-Service | Where-Object { $_.Name -match "MSSQL|SQL" }

# Kiểm tra LocalDB
SqlLocalDB info
```

Nếu không thấy `MSSQL$SQLEXPRESS` và `SqlLocalDB info` không có, cần cài một trong hai:
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-express-download)
- LocalDB (có sẵn cùng Visual Studio)

### 4. Tạo Database

Sau khi sửa xong `Server=`, chạy ở thư mục gốc dự án:

```bash
dotnet tool restore          # cài dotnet-ef đúng phiên bản 10.0.12
dotnet ef database update   # tạo database + 7 bảng từ Migration
```

Database `QLyDatPhongKhachSanDB` sẽ có các bảng:

```
TaiKhoan, LoaiPhong, Phong, KhachHang, DatPhong, ChiTietDatPhong, DichVu
```

Kiểm tra database đã tạo:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d QLyDatPhongKhachSanDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'" -W
```

### 5. Thêm dữ liệu mẫu

Migration chỉ tạo cấu trúc bảng, chưa có dữ liệu. Thêm dữ liệu bằng 1 trong 2 cách:

**Cách 1 — dùng SQL Server Management Studio:**
`SQL Server Object Explorer` → phải chuột `QLyDatPhongKhachSanDB` → `Scripts for Database` → `New Database Script`,
dán nội dung file `InsertData.sql` vào cửa sổ truy vấn rồi chạy.

**Cách 2 — chạy bằng `sqlcmd` ở thư mục gốc dự án:**
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d QLyDatPhongKhachSanDB -i InsertData.sql
```

> 📌 Danh sách tài khoản kiểm thử và số lượng dữ liệu mẫu xem ở các mục bên dưới.

### 6. Lỗi kết nối thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| `Cannot open database` | Database chưa được tạo | Chạy `dotnet ef database update` |
| `Login failed for user` | Sai tên instance | Kiểm tra lại `Get-Service \| Where-Object { $_.Name -match "MSSQL" }` |
| `A network-related or instance-specific error` | LocalDB chưa chạy | Chạy `SqlLocalDB start MSSQLLocalDB` |
| `certificate chain was issued by an authority` | Thiếu TrustServerCertificate | Giữ nguyên `TrustServerCertificate=True` trong chuỗi |
| `provider: SqlClient ... error 40` | SQL Browser service đang tắt | `Start-Service SQLBrowser` |

---

## 👥 Phân Công Module

| Module | Sinh viên | Nội dung |
|--------|-----------|----------|
| **Module 1** | SV1 | Tài khoản - Đăng nhập - Phân quyền - Quản lý loại phòng |
| **Module 2** | SV2 | Quản lý phòng - Tìm kiếm - Lọc - Sắp xếp - Phân trang |
| **Module 3** | SV3 | Quản lý khách hàng - Hồ sơ cá nhân - Đặt phòng - Theo dõi |
| **Module 4** | SV4 | Tiếp nhận đặt phòng - Nhận phòng - Trả phòng - Quản lý trạng thái |
| **Module 5** | SV5 | Tính tiền phòng - Dashboard - Thống kê - Báo cáo |

---

## 📘 Hướng Dẫn Làm Việc Với GitHub

### Bước 1: Cài Đặt Git

1. Tải Git tại: https://git-scm.com/downloads
2. Cài đặt với các tùy chọn mặc định
3. Mở **Git Bash** (hoặc Terminal) và cấu hình thông tin cá nhân:

```bash
# Cấu hình tên (dùng tên thật)
git config --global user.name "Nguyen Van A"

# Cấu hình email (dùng email GitHub)
git config --global user.email "nguyenvana@email.com"

# Kiểm tra cấu hình
git config --list
```

---

### Bước 2: Clone Dự Án

> 🎯 **Clone** = Tải toàn bộ dự án từ GitHub về máy tính của bạn.

```bash
# Clone dự án về máy
git clone https://github.com/<username>/QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN.git

# Di chuyển vào thư mục dự án
cd QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN
```

**Bằng Visual Studio:**
1. Mở Visual Studio → `Clone a repository`
2. Dán URL repository
3. Chọn đường dẫn lưu trữ → Click **Clone**

---

### Bước 3: Tạo Nhánh (Branch)

> 🎯 **Branch** = Nhánh riêng để làm việc, không ảnh hưởng đến code chung.  
> ⚠️ **KHÔNG BAO GIỜ code trực tiếp trên nhánh `main`!**

#### Quy tắc đặt tên nhánh:

```
feature/<mã-sv>-<module>-<mô-tả-ngắn>
```

#### Ví dụ:

```bash
feature/22103100001-module1-dang-nhap
feature/22103100002-module2-phan-trang
feature/22103100003-module3-dat-phong
feature/22103100004-module4-nhan-phong
feature/22103100005-module5-dashboard
```

#### Cách tạo nhánh:

```bash
# Đảm bảo đang ở nhánh main và đã cập nhật code mới nhất
git checkout main
git pull origin main

# Tạo nhánh mới và chuyển sang nhánh đó
git checkout -b feature/22103100001-module1-dang-nhap

# Kiểm tra đang ở nhánh nào
git branch
```

**Bằng Visual Studio:**
1. Thanh trạng thái dưới cùng → Click tên nhánh hiện tại
2. Chọn **New Branch** → Nhập tên nhánh
3. Tick ✅ **Checkout branch** → Click **Create**

---

### Bước 4: Commit & Push

> 🎯 **Commit** = Lưu lại các thay đổi vào lịch sử Git.  
> 🎯 **Push** = Đẩy commit lên GitHub.

```bash
# Xem các file đã thay đổi
git status

# Thêm tất cả file thay đổi vào staging
git add .

# Hoặc thêm từng file cụ thể
git add Models/TaiKhoan.cs
git add Controllers/TaiKhoanController.cs

# Commit với message theo quy định
git commit -m "[22103100001] [TaiKhoan] Tao Entity TaiKhoan va Data Annotation"

# Push lên GitHub (lần đầu push nhánh mới)
git push -u origin feature/22103100001-module1-dang-nhap

# Các lần push tiếp theo
git push
```

**Bằng Visual Studio:**
1. `View` → `Git Changes` (hoặc `Ctrl+Shift+G`)
2. Xem danh sách file thay đổi
3. Nhập commit message → Click **Commit All**
4. Click **Push** (mũi tên ↑)

> ⚠️ **QUAN TRỌNG:** Mỗi thành viên phải có nhiều commit phù hợp tiến độ.  
> **KHÔNG** dồn toàn bộ code vào 1 commit cuối cùng!

---

### Bước 5: Tạo Pull Request (PR)

> 🎯 **Pull Request** = Yêu cầu merge code từ nhánh của bạn vào nhánh `main`.

#### Cách 1: Trên GitHub Website

1. Vào trang repository trên GitHub
2. Click **"Compare & pull request"** (xuất hiện sau khi push nhánh mới)  
   Hoặc: Tab **Pull requests** → **New pull request**
3. Chọn:
   - **base:** `main` (nhánh đích)
   - **compare:** `feature/22103100001-module1-dang-nhap` (nhánh của bạn)
4. Điền thông tin:
   - **Title:** `[22103100001] [Module 1] Hoàn thành chức năng đăng nhập và phân quyền`
   - **Description:** Mô tả chi tiết những gì đã làm
5. Click **Create pull request**
6. Chờ review và approve từ nhóm trưởng
7. Sau khi approve → Click **Merge pull request** → **Confirm merge**

#### Mẫu mô tả Pull Request:

```markdown
## Mô tả
- Tạo Entity TaiKhoan với Data Annotation
- Xây dựng chức năng đăng nhập bằng Session
- Phân quyền tại Controller cho 3 vai trò
- Tạo View đăng nhập với Validation Message

## Checklist
- [ ] Đã test đăng nhập đúng
- [ ] Đã test sai mật khẩu
- [ ] Đã test tài khoản bị khóa
- [ ] Đã test phân quyền tại Controller
- [ ] Code có comment Họ tên + Mã SV

## Screenshots
(Đính kèm ảnh chụp màn hình nếu có)
```

---

### Bước 6: Pull Code Mới Nhất

> 🎯 **Pull** = Tải code mới nhất từ GitHub về máy.  
> ⚠️ **Luôn pull trước khi bắt đầu code!**

```bash
# Chuyển về nhánh main
git checkout main

# Pull code mới nhất
git pull origin main

# Chuyển sang nhánh của bạn
git checkout feature/22103100001-module1-dang-nhap

# Cập nhật nhánh của bạn với code mới nhất từ main
git merge main
```

**Quy trình mỗi ngày bắt đầu code:**
```bash
# 1. Lưu công việc hiện tại (nếu có)
git add .
git commit -m "[MaSV] [Module] Luu cong viec hien tai"

# 2. Cập nhật main
git checkout main
git pull origin main

# 3. Quay lại nhánh và merge main vào
git checkout feature/<ten-nhanh-cua-ban>
git merge main

# 4. Tiếp tục code
```

---

### Bước 7: Xử Lý Conflict (Xung Đột)

> 🎯 **Conflict** xảy ra khi 2 người sửa cùng 1 file, cùng 1 dòng.

Khi merge hoặc pull mà có conflict, Git sẽ đánh dấu trong file:

```csharp
<<<<<<< HEAD
// Code của bạn
public string TrangThai { get; set; } = "HoatDong";
=======
// Code của người khác
public string TrangThai { get; set; } = "Active";
>>>>>>> main
```

**Cách xử lý:**

1. Mở file bị conflict
2. Tìm các đánh dấu `<<<<<<<`, `=======`, `>>>>>>>`
3. Chọn giữ code nào (hoặc kết hợp cả hai)
4. Xóa các dòng đánh dấu
5. Save file

```bash
# Sau khi sửa xong conflict
git add .
git commit -m "[MaSV] [Module] Resolve merge conflict"
git push
```

**Bằng Visual Studio:**
1. Visual Studio sẽ hiện **Merge Editor** khi có conflict
2. Chọn **Accept Current** (giữ code của bạn) hoặc **Accept Incoming** (lấy code từ main)
3. Hoặc chọn **Accept Both** và chỉnh sửa thủ công
4. Save → Commit

> 💡 **Mẹo tránh Conflict:**
> - Mỗi người làm việc trên **file riêng** (theo module phân công)
> - **Pull code thường xuyên** (ít nhất 1 lần/ngày)
> - **Không sửa file của người khác** nếu không cần thiết
> - Thống nhất Entity & DbContext **trước khi code**

---

## 📝 Quy Định Commit Message

### Format:
```
[Mã SV] [Module] Nội dung công việc
```

### Ví dụ cụ thể cho từng Module:

**Module 1 (SV1):**
```
[22103100001] [TaiKhoan] Tao Entity TaiKhoan voi Data Annotation
[22103100001] [TaiKhoan] Xay dung chuc nang dang nhap bang Session
[22103100001] [TaiKhoan] Phan quyen tai Controller cho 3 vai tro
[22103100001] [TaiKhoan] Them Validation ten dang nhap khong trung
[22103100001] [LoaiPhong] Tao Entity LoaiPhong
[22103100001] [LoaiPhong] Hoan thanh CRUD LoaiPhong
```

**Module 2 (SV2):**
```
[22103100002] [Phong] Tao Entity Phong voi FK LoaiPhong
[22103100002] [Phong] Hoan thanh CRUD Phong
[22103100002] [Phong] Them chuc nang tim kiem theo so phong va ten loai
[22103100002] [Phong] Them chuc nang loc theo loai phong va trang thai
[22103100002] [Phong] Them sap xep theo don gia va tang
[22103100002] [Phong] Them phan trang voi Skip Take
[22103100002] [Phong] Ket hop Search Filter Sort Pagination
```

**Module 3 (SV3):**
```
[22103100003] [KhachHang] Tao Entity KhachHang
[22103100003] [DatPhong] Tao Entity DatPhong
[22103100003] [KhachHang] Ho so ca nhan chi xem sua cua chinh minh
[22103100003] [DatPhong] Tao chuc nang dat phong kem kiem tra dieu kien
[22103100003] [DatPhong] Them chuc nang huy dat phong
[22103100003] [DatPhong] Theo doi trang thai dat phong
```

**Module 4 (SV4):**
```
[22103100004] [TiepNhan] Danh sach dat phong can xu ly
[22103100004] [TiepNhan] Xu ly trang thai nhan phong
[22103100004] [TiepNhan] Xu ly trang thai tra phong
[22103100004] [TiepNhan] Kiem tra trung lich dat phong bang LINQ
[22103100004] [TiepNhan] Kiem tra suc chua phong truoc khi dat
```

**Module 5 (SV5):**
```
[22103100005] [ChiTietDatPhong] Tao Entity ChiTietDatPhong
[22103100005] [ChiTietDatPhong] Tinh tien phong theo so ngay
[22103100005] [ThongKe] Tao Dashboard tong quan
[22103100005] [ThongKe] Thong ke so phong theo loai bang LINQ GroupBy
[22103100005] [ThongKe] Thong ke doanh thu theo thang
[22103100005] [ThongKe] Them bieu do thong ke
```

> ⚠️ **LƯU Ý:** Commit message phải viết **KHÔNG DẤU** để tránh lỗi encoding!

---

## 🔄 Quy Trình Làm Việc Tổng Thể

```
┌─────────────────────────────────────────────────────────────┐
│                    QUY TRÌNH LÀM VIỆC                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 📐 Thiết kế CSDL (cả nhóm thống nhất)                 │
│         ↓                                                   │
│  2. 🏗️  Tạo Project + Repository (SV1 - nhóm trưởng)      │
│         ↓                                                   │
│  3. 📦 Tạo Entity + DbContext + Migration (cả nhóm)        │
│         ↓                                                   │
│  4. 🌿 Mỗi SV tạo nhánh riêng theo module                 │
│         ↓                                                   │
│  5. 💻 Code trên nhánh riêng + Commit thường xuyên         │
│         ↓                                                   │
│  6. ⬆️  Push lên GitHub                                    │
│         ↓                                                   │
│  7. 📋 Tạo Pull Request                                    │
│         ↓                                                   │
│  8. 👀 Review + Merge vào main                             │
│         ↓                                                   │
│  9. ⬇️  Cả nhóm Pull code mới nhất                        │
│         ↓                                                   │
│ 10. 🧪 Kiểm thử + Fix bugs                                │
│         ↓                                                   │
│ 11. ✅ Hoàn thiện + Nộp                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Migration khi làm nhóm:

> ⚠️ **QUY ĐỊNH QUAN TRỌNG:**
> - Thống nhất thay đổi Entity **TRƯỚC** khi tạo Migration
> - Migration có tên rõ nghĩa và được Commit lên Repository
> - **KHÔNG** tự ý thay đổi Entity gây xung đột
> - Chỉ **1 người** tạo Migration tại 1 thời điểm

```bash
# Tạo Migration (chỉ khi cả nhóm đã thống nhất)
dotnet ef migrations add TenMigration

# Cập nhật Database
dotnet ef database update

# Commit Migration
git add Migrations/
git commit -m "[MaSV] [Migration] Tao Migration TenMigration"
git push
```

---

## 🗂 Cấu Trúc Thư Mục

```
QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN/
├── README.md
├── .gitignore
├── QLyDatPhongKhachSan.sln
└── QLyDatPhongKhachSan/
    ├── Models/                    ← Entity classes
    │   ├── TaiKhoan.cs           (SV1)
    │   ├── LoaiPhong.cs          (SV1)
    │   ├── Phong.cs              (SV2)
    │   ├── KhachHang.cs          (SV3)
    │   ├── DatPhong.cs           (SV3)
    │   ├── ChiTietDatPhong.cs    (SV5)
    │   └── DichVu.cs             (SV5)
    ├── ViewModels/                ← ViewModels
    ├── Data/
    │   └── AppDbContext.cs        (Cả nhóm)
    ├── Controllers/               ← Controllers
    ├── Views/                     ← Razor Views
    ├── Migrations/                ← EF Core Migrations
    ├── wwwroot/                   ← Static files (CSS, JS, images)
    ├── Program.cs
    └── appsettings.json
```

---

## 🔑 Tài Khoản Kiểm Thử

| Vai trò | Tên đăng nhập | Mật khẩu | Ghi chú |
|---------|---------------|----------|---------|
| Admin | `admin` | `Admin@123` | Quản lý toàn hệ thống |
| Nhân viên | `nhanvien01` | `Nv@123` | Lễ tân |
| Nhân viên | `nhanvien02` | `Nv@123` | Lễ tân |
| Khách hàng | `khachhang01` | `Kh@123` | Khách hàng mẫu |
| Khách hàng bị khóa | `khachhang_locked` | `Kh@123` | Test tài khoản bị khóa |

---

## 📊 Dữ Liệu Mẫu

| Loại dữ liệu | Số lượng |
|---------------|----------|
| Loại phòng | 05 |
| Phòng | 15+ (nhiều trạng thái) |
| Khách hàng | 30+ |
| Tài khoản Admin | 01-02 |
| Tài khoản Nhân viên | 02-03 |
| Giao dịch đặt phòng | 40-50 (nhiều trạng thái) |
| Chi tiết đặt phòng | 15-20 |

---

## 📌 Lệnh Git Thường Dùng (Cheatsheet)

| Lệnh | Mô tả |
|-------|-------|
| `git clone <url>` | Tải dự án về máy |
| `git status` | Xem trạng thái file |
| `git add .` | Thêm tất cả file vào staging |
| `git commit -m "message"` | Commit với message |
| `git push` | Đẩy code lên GitHub |
| `git pull origin main` | Lấy code mới nhất từ main |
| `git checkout main` | Chuyển sang nhánh main |
| `git checkout -b <tên-nhánh>` | Tạo & chuyển sang nhánh mới |
| `git branch` | Xem danh sách nhánh |
| `git branch -d <tên-nhánh>` | Xóa nhánh (đã merge) |
| `git merge main` | Merge code từ main vào nhánh hiện tại |
| `git log --oneline -10` | Xem 10 commit gần nhất |
| `git stash` | Lưu tạm thay đổi chưa commit |
| `git stash pop` | Khôi phục thay đổi đã stash |

---

## 🆘 Xử Lý Sự Cố Thường Gặp

### ❌ Push bị reject

```bash
# Nguyên nhân: code trên GitHub mới hơn máy bạn
# Giải pháp:
git pull origin <tên-nhánh>
# Xử lý conflict nếu có
git push
```

### ❌ Lỡ commit trên nhánh main

```bash
# Chuyển commit sang nhánh mới
git branch feature/ten-nhanh-moi
git reset --hard HEAD~1     # Xóa commit khỏi main (⚠️ cẩn thận)
git checkout feature/ten-nhanh-moi
```

### ❌ Muốn hủy thay đổi chưa commit

```bash
# Hủy thay đổi 1 file
git checkout -- <tên-file>

# Hủy tất cả thay đổi
git checkout -- .
```

### ❌ Xóa nhánh đã merge

```bash
# Xóa nhánh local
git branch -d feature/ten-nhanh

# Xóa nhánh trên GitHub
git push origin --delete feature/ten-nhanh
```

---

## 📄 Ghi Comment Mã Nguồn

Mỗi file code phải có header:

```csharp
// Họ và tên: Nguyễn Văn A
// Mã sinh viên: 22103100001
// Nội dung thực hiện: Quản lý tài khoản, đăng nhập,
// phân quyền và quản lý loại phòng.
```

> Áp dụng cho: **Model**, **Controller**, **ViewModel**, **View**, và các lớp xử lý khác.

---

## 📌 Ghi Chú Quan Trọng

### Về Database

1. **Ai cũng phải tự tạo database trên máy mình.** Database không được commit lên GitHub.
   ```bash
   dotnet tool restore
   dotnet ef database update
   ```

2. **Không đổi tên database.** Tên phải luôn là `QLyDatPhongKhachSanDB` để cả nhóm thống nhất.
   Chỉ được đổi đoạn `Server=` trong `appsettings.json`.

3. **`appsettings.json` được commit lên GitHub.** Vì vậy chỉ chứa thông tin kết nối cục bộ,
   tuyệt đối **không** đặt mật khẩu hay connection string có mật khẩu thật vào file này.
   Nếu cần bảo mật hơn, dùng User Secrets:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=QLyDatPhongKhachSanDB;..."
   ```

4. **Dữ liệu trên mỗi máy là độc lập.** Sửa dữ liệu trên máy bạn không ảnh hưởng tới máy người khác.
   Muốn đồng bộ thì dùng file script `.sql` và chạy trên từng máy.

5. **Migration không kèm dữ liệu mẫu.** Sau khi `dotnet ef database update`, database còn trống.
   Chạy thêm `InsertData.sql` mới có tài khoản đăng nhập để test (xem mục *Kết nối Database*).

### Về công cụ EF Core

6. **Luôn dùng `dotnet-ef` từ local tool manifest** (`dotnet-tools.json`), không dùng bản global.
   Dự án cần đúng phiên bản `10.0.12` — bản global có thể cũ hơn và sẽ báo lỗi
   *"Tools older than runtime"*.
   ```bash
   dotnet tool restore
   ```

7. **Chỉ 1 người tạo Migration tại 1 thời điểm** để tránh xung đột khi merge.

### Về code

8. **Không commit các file sinh tự động** — `.gitignore` đã loại sẵn `bin/`, `obj/`, `appsettings.*.local.json`.
   Kiểm tra trước khi commit:
   ```bash
   git status
   ```

9. **Không sửa Entity của người khác** khi không cần thiết. Cần thay đổi thì báo nhóm trưởng trước.

10. **Không dồn toàn bộ code vào 1 commit cuối cùng.** Mỗi thành viên phải có nhiều commit theo tiến độ.

### Thông tin phiên bản

| Thành phần | Phiên bản |
|------------|-----------|
| .NET SDK | 10.0.401 |
| Entity Framework Core | 10.0.12 |
| ASP.NET Core | 10.0 |
| dotnet-ef tool | 10.0.12 |

---

## 📞 Liên Hệ

Nếu gặp vấn đề với Git/GitHub, liên hệ nhóm trưởng hoặc trao đổi trong group chat nhóm.

---

*© 2026 - Nhóm 2 - DHTI17A2HN - UNETI*
