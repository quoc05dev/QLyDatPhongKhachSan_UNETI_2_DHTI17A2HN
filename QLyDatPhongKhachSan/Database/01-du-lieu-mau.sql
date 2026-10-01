/* ============================================================================
   ĐỀ TÀI 15 - HỆ THỐNG QUẢN LÝ ĐẶT PHÒNG KHÁCH SẠN
   DỮ LIỆU MẪU PHỤC VỤ KIỂM THỬ (không phải CRUD bắt buộc)

   Họ và tên  : Đặng Minh Quốc
   Mã SV      : 23103100069 - Module 5 (Dashboard & Thống kê)
   Căn cứ     : De_15.docx mục 13 (Dữ liệu đầu vào) và E2 trong kế hoạch dự án

   MỤC ĐÍCH
   Bảng LoaiPhong / Phong / TaiKhoan / KhachHang / DatPhong / ChiTietDatPhong
   ban đầu rỗng nên Dashboard và các báo cáo thống kê không có số liệu để kiểm
   chứng. Script này tạo dữ liệu giả lập trải trên nhiều tháng, đủ cả 5 trạng
   thái giao dịch để đối chiếu kết quả LINQ với truy vấn SQL thuần.

   CÁCH CHẠY
     sqlcmd -S "(localdb)\MSSQLLocalDB" -d QLyDatPhongKhachSanDB -f 65001 -i <duong-dan-file>

   LƯU Ý KỸ THUẬT
   - sqlcmd mặc định để QUOTED_IDENTIFIER ở trạng thái OFF, trong khi các index
     do EF Core tạo yêu cầu ON. Vì vậy script bắt buộc phải bật lại bằng
     câu lệnh SET ở phần đầu, nếu không sẽ báo lỗi Msg 1934.
   - Script chèn dữ liệu bằng SET IDENTITY_INSERT ON kèm khóa chính tường minh.
     Nếu chỉ để SQL Server tự sinh khóa thì chạy lần hai sẽ ra khóa khác và các
     ràng buộc khóa ngoại giữa các bảng sẽ hỏng. Cách này giúp script chạy lại
     bao nhiêu lần cũng cho ra cùng một bộ dữ liệu.

   LƯU Ý AN TOÀN
   - Script XÓA TOÀN BỘ dữ liệu đang có trong 6 bảng trên trước khi thêm mới.
     Chỉ chạy trên database phát triển cá nhân, KHÔNG chạy trên database dùng
     để trình bày với giảng viên sau khi đã nhập dữ liệu thật.
   - Không đụng tới cấu trúc bảng: mọi thay đổi schema đều nằm trong Migration
     của EF Core, đúng yêu cầu mục 11.1 của đề.
   - Muốn xóa sạch dữ liệu mẫu thì chạy file 02-xoa-du-lieu-mau.sql.
   ============================================================================ */

SET NOCOUNT ON;
-- Bắt buộc bật lại vì sqlcmd mặc định đặt QUOTED_IDENTIFIER ở trạng thái OFF,
-- còn các unique index do EF Core tạo (IX_TaiKhoan_TenDangNhap,
-- IX_Phong_SoPhong, IX_KhachHang_MaTaiKhoan) yêu cầu ON.
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------------
   1. XÓA DỮ LIỆU CŨ - thứ tự tuân thủ ràng buộc khóa ngoại
   --------------------------------------------------------------------------- */

BEGIN TRANSACTION;

-- Chi tiết trỏ tới DatPhong và Phong, phải xóa trước
DELETE FROM ChiTietDatPhong;
-- DatPhong trỏ tới KhachHang
DELETE FROM DatPhong;
-- KhachHang trỏ tới TaiKhoan, phải xóa trước TaiKhoan
DELETE FROM KhachHang;
-- Phong trỏ tới LoaiPhong
DELETE FROM Phong;
DELETE FROM LoaiPhong;
DELETE FROM TaiKhoan;
DELETE FROM DichVu;

COMMIT TRANSACTION;
GO

/* ---------------------------------------------------------------------------
   2. TÀI KHOẢN (mục 5.1) - gồm 1 tài khoản bị khóa để kiểm tra ràng buộc 5.2
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT TaiKhoan ON;

INSERT INTO TaiKhoan (MaTaiKhoan, TenDangNhap, MatKhau, HoTen, Email, SoDienThoai, VaiTro, TrangThai, NgayTao)
VALUES
    (1, 'admin',     '123456', N'Quản Trị Viên',      'admin@hotel.com',     '0901234567', 'Admin',     1, '2026-01-05'),
    (2, 'nhanvien',  '123456', N'Nguyễn Thị Lan',     'nhanvien@hotel.com',  '0902345678', 'NhanVien',  1, '2026-01-05'),
    (3, 'khachhang', '123456', N'Lê Minh Anh',        'khachhang@gmail.com', '0903456789', 'KhachHang', 1, '2026-01-05'),
    (4, 'khoa',      '123456', N'Tài Khoản Bị Khóa',  'khoa@gmail.com',      '0904567890', 'KhachHang', 0, '2026-01-05');

SET IDENTITY_INSERT TaiKhoan OFF;
GO

/* ---------------------------------------------------------------------------
   3. LOẠI PHÒNG (mục 5.5)
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT LoaiPhong ON;

INSERT INTO LoaiPhong (MaLoaiPhong, TenLoai, SoNguoiToiDa, DonGiaNgay, MoTa, TrangThai)
VALUES
    (1, N'Phòng Tiêu Chuẩn', 2,  450000.00, N'Phòng đơn/nhô đầy đủ tiện nghi cơ bản',          1),
    (2, N'Phòng Nâng Cao',   3,  650000.00, N'Phòng góc, thêm sofa và bàn làm việc',             1),
    (3, N'Phòng Cao Cấp',    3,  900000.00, N'Phòng rộng, ban công riêng và bồn tắm nước nóng',  1),
    (4, N'Phòng Suite',      5, 1500000.00, N'Hai phòng liên thông, phòng khách riêng biệt',   1),
    (5, N'Phòng Hai Phòng',  4, 1200000.00, N'Hai phòng ngủ riêng biệt cho gia đình',           1);

SET IDENTITY_INSERT LoaiPhong OFF;
GO

/* ---------------------------------------------------------------------------
   4. PHÒNG (mục 6.1) - 18 phòng trên 4 tầng
   Trạng thái sẽ được đồng bộ lại ở bước 8 theo giao dịch thực tế
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT Phong ON;

INSERT INTO Phong (MaPhong, SoPhong, MaLoaiPhong, Tang, HuongPhong, TienNghi, DonGia, TrangThai, GhiChu)
VALUES
    -- Tầng 1
    ( 1, '101', 1, 1, N'Hướng vườn', N'Smart TV, Điều hòa, Phòng tắm riêng',      450000.00, N'Trong', N'Gần lối ra vào'),
    ( 2, '102', 1, 1, N'Hướng vườn', N'Smart TV, Điều hòa, Phòng tắm riêng',      450000.00, N'Trong', NULL),
    ( 3, '103', 1, 1, N'Hướng phố',  N'Smart TV, Điều hòa, Phòng tắm riêng',      450000.00, N'Trong', NULL),
    ( 4, '104', 2, 1, N'Hướng phố',  N'Smart TV, Điều hòa, Sofa, Bàn làm việc',   650000.00, N'Trong', NULL),
    ( 5, '105', 2, 1, N'Hướng vườn', N'Smart TV, Điều hòa, Sofa, Bàn làm việc',   650000.00, N'Trong', NULL),
    -- Tầng 2
    ( 6, '201', 2, 2, N'Hướng vườn', N'Smart TV, Điều hòa, Sofa, Bàn làm việc',   650000.00, N'Trong', NULL),
    ( 7, '202', 2, 2, N'Hướng phố',  N'Smart TV, Điều hòa, Sofa, Bàn làm việc',   650000.00, N'Trong', NULL),
    ( 8, '203', 3, 2, N'Hướng phố',  N'Smart TV, Điều hòa, Ban công, Bồn tắm',   900000.00, N'Trong', N'Phòng góc có ban công'),
    ( 9, '204', 3, 2, N'Hướng vườn', N'Smart TV, Điều hòa, Ban công, Bồn tắm',   900000.00, N'Trong', NULL),
    (10, '205', 3, 2, N'Hướng biển', N'Smart TV, Điều hòa, Ban công, Bồn tắm',   900000.00, N'Trong', N'View biển, yêu cầu cao'),
    -- Tầng 3
    (11, '301', 3, 3, N'Hướng phố',  N'Smart TV, Điều hòa, Ban công, Bồn tắm',   900000.00, N'Trong', NULL),
    (12, '302', 3, 3, N'Hướng vườn', N'Smart TV, Điều hòa, Ban công, Bồn tắm',   900000.00, N'Trong', NULL),
    (13, '303', 4, 3, N'Hướng phố',  N'Smart TV, Điều hòa, Sofa, Bồn tắm, Bàn ăn', 1500000.00, N'Trong', N'Suite 2 phòng'),
    (14, '304', 4, 3, N'Hướng biển', N'Smart TV, Điều hòa, Sofa, Bồn tắm, Bàn ăn', 1500000.00, N'Trong', N'Suite 2 phòng, view biển'),
    (15, '305', 4, 3, N'Hướng vườn', N'Smart TV, Điều hòa, Sofa, Bồn tắm, Bàn ăn', 1500000.00, N'Trong', N'Suite 2 phòng'),
    -- Tầng 4
    (16, '401', 5, 4, N'Hướng phố',  N'Smart TV, Điều hòa, 2 giường đôi, Bồn tắm', 1200000.00, N'Trong', N'Phòng 2 phòng ngủ'),
    (17, '402', 5, 4, N'Hướng biển', N'Smart TV, Điều hòa, 2 giường đôi, Bồn tắm', 1200000.00, N'Trong', N'Phòng 2 phòng ngủ, view biển'),
    (18, '403', 4, 4, N'Hướng phố',  N'Smart TV, Điều hòa, Sofa, Bồn tắm, Bàn ăn', 1500000.00, N'Trong', N'Suite 2 phòng');

SET IDENTITY_INSERT Phong OFF;
GO

/* ---------------------------------------------------------------------------
   5. KHÁCH HÀNG (mục 7.1) - 20 hồ sơ, 2 hồ sơ liên kết tài khoản online
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT KhachHang ON;

INSERT INTO KhachHang (MaKhachHang, MaTaiKhoan, HoTen, NgaySinh, GioiTinh, CCCD, SoDienThoai, Email, DiaChi, QuocTich, TrangThai, GhiChu)
VALUES
    ( 1, 3,    N'Lê Minh Anh',         '1990-05-12', N'Nam', '001095001234', '0912000001', 'leminhanh@gmail.com',  N'12 Nguyễn Huệ, Q.1, TP.HCM',      N'Việt Nam', 1, N'Khách quay lại nhiều lần'),
    ( 2, NULL, N'Nguyễn Thị Bích',     '1988-11-03', N'Nữ',  '001088002345', '0912000002', NULL,                   N'45 Lê Lợi, Q.1, TP.HCM',          N'Việt Nam', 1, NULL),
    ( 3, NULL, N'Trần Văn Hùng',       '1995-02-18', N'Nam', '001095003456', '0912000003', NULL,                   N'78 Nguyễn Trãi, Q.5, TP.HCM',     N'Việt Nam', 1, NULL),
    ( 4, NULL, N'Phạm Thị Dung',       '1993-07-25', N'Nữ',  '001093004567', '0912000004', NULL,                   N'23 Hai Bà Trưng, Q.1, TP.HCM',    N'Việt Nam', 1, N'Yêu cầu phòng tầng cao'),
    ( 5, NULL, N'Hoàng Minh Giang',    '1985-09-30', N'Nam', '001085005678', '0912000005', NULL,                   N'150 Võ Văn Tần, Q.3, TP.HCM',     N'Việt Nam', 1, NULL),
    ( 6, NULL, N'Đỗ Thị Lan',          '1997-01-14', N'Nữ',  '001097006789', '0912000006', NULL,                   N'60 Cách Mạng Tháng 8, Q.10, TP.HCM', N'Việt Nam', 1, NULL),
    ( 7, NULL, N'Vũ Đức Hùng',         '1991-04-08', N'Nam', '001091007890', '0912000007', NULL,                   N'34 Lý Thường Kiệt, Q.1, TP.HCM',   N'Việt Nam', 1, NULL),
    ( 8, NULL, N'Bùi Thị Mai',         '1994-12-21', N'Nữ',  '001094008901', '0912000008', NULL,                   N'17 Điện Biên Phủ, Q.3, TP.HCM',   N'Việt Nam', 1, NULL),
    ( 9, NULL, N'Đặng Văn Hải',        '1989-06-16', N'Nam', '001089009012', '0912000009', NULL,                   N'88 Trần Hưng Đạo, Q.1, TP.HCM',   N'Việt Nam', 1, NULL),
    (10, NULL, N'Ngô Thị Hồng',        '1996-08-05', N'Nữ',  '001096010123', '0912000010', NULL,                   N'29 Pasteur, Q.1, TP.HCM',          N'Việt Nam', 1, NULL),
    (11, NULL, N'Dương Văn Cường',     '1983-03-11', N'Nam', '001083011234', '0912000011', NULL,                   N'11 Lê Duẩn, Q.1, TP.HCM',         N'Việt Nam', 1, N'Khách doanh nghiệp'),
    (12, NULL, N'Lý Thị Hồng Nhung',   '1992-10-27', N'Nữ',  '001092012345', '0912000012', NULL,                   N'52 Phó Đức Chính, Q.1, TP.HCM',   N'Việt Nam', 1, NULL),
    (13, NULL, N'Trịnh Văn Phúc',      '1990-05-09', N'Nam', '001090013456', '0912000013', NULL,                   N'95 Bà Triệu, Q.1, TP.HCM',        N'Việt Nam', 1, NULL),
    (14, NULL, N'Phan Thị Quỳnh',      '1997-07-19', N'Nữ',  '001097014567', '0912000014', NULL,                   N'36 Tôn Đức Thắng, Q.1, TP.HCM',   N'Việt Nam', 1, NULL),
    (15, NULL, N'Hà Văn Sơn',          '1986-02-13', N'Nam', '001086015678', '0912000015', NULL,                   N'74 Nguyễn Đình Chiểu, Q.3, TP.HCM', N'Việt Nam', 1, NULL),
    (16, NULL, N'Võ Thị Thu',          '1993-09-22', N'Nữ',  '001093016789', '0912000016', NULL,                   N'28 Tôn Thất Hiệp, Q.10, TP.HCM',   N'Việt Nam', 1, NULL),
    (17, NULL, N'Cao Văn Long',        '1987-11-30', N'Nam', '001087017890', '0912000017', NULL,                   N'19 Phạm Ngũ Lão, Q.5, TP.HCM',    N'Việt Nam', 1, NULL),
    (18, NULL, N'Chu Thị Hạnh',        '1995-04-06', N'Nữ',  '001095018901', '0912000018', NULL,                   N'43 Trần Quốc Thảo, Q.3, TP.HCM',  N'Việt Nam', 1, NULL),
    (19, NULL, N'Đinh Văn Kiên',       '1992-12-01', N'Nam', '001092019012', '0912000019', NULL,                   N'66 Cách Mạng Tháng 8, Q.10, TP.HCM', N'Việt Nam', 1, NULL),
    (20, NULL, N'Lương Thị Hạnh Nhung', '1998-06-17', N'Nữ',  '001098020123', '0912000020', NULL,                   N'7 Nguyễn Đình Chiểu, Q.3, TP.HCM', N'Việt Nam', 1, NULL);

SET IDENTITY_INSERT KhachHang OFF;
GO

/* ---------------------------------------------------------------------------
   6. ĐƠN ĐẶT PHÒNG (mục 8.1) - 40 đơn trải trên 6 tháng (05/2026 - 10/2026)
      Tháng 05-09 đã xử lý xong (phần lớn HoanThanh, có DaHuy)
      Tháng 10 còn đang mở: DangSuDung / ChoXuLy / DangXuLy / DaHuy
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT DatPhong ON;

-- @MaDon tăng liên tục qua các tháng, nhờ vậy MaDatPhong luôn là 1..40 và phòng
-- được gán xoay vòng ở bước 7 luôn hợp lệ.
DECLARE @MaDon INT = 1;

DECLARE @Thang INT = 5;

WHILE @Thang <= 10
BEGIN
    DECLARE @NgayDau DATE = DATEFROMPARTS(2026, @Thang, 1);

    -- Số đơn trong từng tháng
    DECLARE @SoDon INT = CASE @Thang
        WHEN 5 THEN 7
        WHEN 6 THEN 8
        WHEN 7 THEN 6
        WHEN 8 THEN 7
        WHEN 9 THEN 6
        ELSE 6
    END;

    DECLARE @i INT = 1;

    WHILE @i <= @SoDon
    BEGIN
        -- Phân bố trạng thái:
        --   Tháng đã qua: cứ 7 đơn thì 1 đơn bị hủy, còn lại hoàn thành
        --   Tháng hiện tại: đơn đầu đã nhận phòng, các đơn sau trải đều 4 trạng
        --   thái còn lại của vòng đời đặt phòng
        DECLARE @TrangThai NVARCHAR(50);

        IF @Thang < 10
        BEGIN
            SET @TrangThai = CASE WHEN @i % 7 = 0 THEN N'DaHuy' ELSE N'HoanThanh' END;
        END
        ELSE
        BEGIN
            SET @TrangThai = CASE @i
                WHEN 1 THEN N'DangSuDung'
                WHEN 2 THEN N'DangSuDung'
                WHEN 3 THEN N'DangSuDung'
                WHEN 4 THEN N'ChoXuLy'
                WHEN 5 THEN N'DangXuLy'
                WHEN 6 THEN N'DaXacNhan'
                ELSE N'DaHuy'
            END;
        END

        -- Ngày đặt trước ngày nhận 1-3 ngày; ngày nhận rơi trong tháng
        DECLARE @Lech INT = (@i % 3) + 1;
        DECLARE @NgayNhan DATE = DATEADD(DAY, @i - 1, @NgayDau);
        DECLARE @NgayTra DATE = DATEADD(DAY, 2, @NgayNhan);

        INSERT INTO DatPhong (MaDatPhong, MaKhachHang, NgayDat, NgayNhan, NgayTraDuKien, SoNguoi, TienCoc, TongTien, TrangThai, GhiChu)
        VALUES
        (
            @MaDon,
            ((@Thang * 7 + @i) % 20) + 1,
            DATEADD(DAY, -@Lech, @NgayNhan),
            @NgayNhan,
            @NgayTra,
            CASE WHEN @i % 4 = 0 THEN 3 WHEN @i % 3 = 0 THEN 2 ELSE 1 END,
            500000.00,
            0,
            @TrangThai,
            CASE @TrangThai
                WHEN N'DaHuy'      THEN N'Khách hủy do thay đổi kế hoạch'
                WHEN N'HoanThanh'  THEN NULL
                WHEN N'DangSuDung' THEN N'Khách đang lưu trú'
                ELSE N'Yêu cầu check-in sau 14h'
            END
        );

        SET @i = @i + 1;
        SET @MaDon = @MaDon + 1;
    END

    SET @Thang = @Thang + 1;
END

SET IDENTITY_INSERT DatPhong OFF;
GO

/* ---------------------------------------------------------------------------
   7. CHI TIẾT ĐẶT PHÒNG (mục 9.1) + áp dụng công thức của SV5
      Thành tiền = Số ngày ở x Đơn giá
      Mỗi đơn ứng với 1 phòng. Phòng được gán xoay vòng theo MaDatPhong nên
      trong cùng tháng không có 2 đơn nào trùng phòng.
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT ChiTietDatPhong ON;

INSERT INTO ChiTietDatPhong (MaChiTiet, MaDatPhong, MaPhong, NgayNhan, NgayTra, DonGia, SoNgay, ThanhTien, TrangThai)
SELECT
    dp.MaDatPhong,
    dp.MaDatPhong,
    p.MaPhong,
    dp.NgayNhan,
-- Đơn đang lưu trú thì ngày trả chưa xảy ra, đặt trước để tạo doanh thu
        -- đang phát sinh. Đơn bị hủy chưa nhận phòng nên ngày nhận và ngày trả
        -- trùng nhau, công thức tự quy về tối thiểu 1 ngày.
        CASE
            WHEN dp.TrangThai = N'DangSuDung' THEN DATEADD(DAY, 9, dp.NgayNhan)
            WHEN dp.TrangThai = N'DaHuy'      THEN dp.NgayNhan
            ELSE dp.NgayTraDuKien
        END,
        p.DonGia,
        ngay.SoNgay,
        ngay.SoNgay * p.DonGia,
    CASE dp.TrangThai
        WHEN N'HoanThanh'  THEN N'DaTra'
        WHEN N'DangSuDung' THEN N'DaNhan'
        WHEN N'DaHuy'      THEN N'DaHuy'
        ELSE N'ChoNhan'
    END
FROM DatPhong dp
INNER JOIN Phong p ON p.MaPhong = ((dp.MaDatPhong - 1) % 18) + 1
CROSS APPLY
(
    -- Công thức giống ChiTietDatPhongController.ApDungCongThucTinhTien():
    -- số ngày ở tối thiểu 1 ngày, thành tiền làm tròn 2 chữ số thập phân
SELECT
        CASE
            WHEN DATEDIFF(DAY, dp.NgayNhan,
                 CASE
                     WHEN dp.TrangThai = N'DangSuDung' THEN DATEADD(DAY, 9, dp.NgayNhan)
                     WHEN dp.TrangThai = N'DaHuy'      THEN dp.NgayNhan
                     ELSE dp.NgayTraDuKien
                 END) < 1
            THEN 1
            ELSE DATEDIFF(DAY, dp.NgayNhan,
                 CASE
                     WHEN dp.TrangThai = N'DangSuDung' THEN DATEADD(DAY, 9, dp.NgayNhan)
                     WHEN dp.TrangThai = N'DaHuy'      THEN dp.NgayNhan
                     ELSE dp.NgayTraDuKien
                 END)
        END AS SoNgay
) ngay;

SET IDENTITY_INSERT ChiTietDatPhong OFF;
GO

/* ---------------------------------------------------------------------------
   8. Cập nhật Tổng tiền của hóa đơn (mục 9.2)
      Tổng tiền = tổng thành tiền các chi tiết chưa bị hủy
   --------------------------------------------------------------------------- */

-- Đơn bị hủy thì không phát sinh doanh thu, nên Tổng tiền phải bằng 0 cho khớp
-- với cách ChiTietDatPhongController.TinhTongTien() bỏ qua chi tiết đã hủy.
UPDATE dp
SET dp.TongTien = CASE WHEN dp.TrangThai = N'DaHuy' THEN 0 ELSE tong.TongTien END
FROM DatPhong dp
CROSS APPLY
(
    SELECT ISNULL(SUM(ct.ThanhTien), 0) AS TongTien
    FROM ChiTietDatPhong ct
    WHERE ct.MaDatPhong = dp.MaDatPhong
      AND ct.TrangThai <> N'DaHuy'
) tong;
GO

/* ---------------------------------------------------------------------------
   9. Đồng bộ trạng thái phòng theo giao dịch thực tế (mục 6.1)
      - Phòng có khách đang ở        -> DangSuDung
      - Phòng đã có đơn chờ xử lý   -> DangXuLy
      - 2 phòng cuối đưa vào bảo trì -> BaoTri
      - Còn lại                      -> Trong
   --------------------------------------------------------------------------- */

-- Phòng đưa vào bảo trì được ưu tiên đặt trước, để luôn có ít nhất 2 phòng
-- ở trạng thái này dù phòng đó đã có lịch đặt trước đó.
UPDATE p
SET p.TrangThai = CASE
        WHEN p.SoPhong IN ('403', '105') THEN N'BaoTri'
        WHEN EXISTS (
            SELECT 1 FROM ChiTietDatPhong ct
            INNER JOIN DatPhong dp ON dp.MaDatPhong = ct.MaDatPhong
            WHERE ct.MaPhong = p.MaPhong AND dp.TrangThai = N'DangSuDung'
        ) THEN N'DangSuDung'
        WHEN EXISTS (
            SELECT 1 FROM ChiTietDatPhong ct
            INNER JOIN DatPhong dp ON dp.MaDatPhong = ct.MaDatPhong
            WHERE ct.MaPhong = p.MaPhong AND dp.TrangThai IN (N'ChoXuLy', N'DangXuLy', N'DaXacNhan')
        ) THEN N'DangXuLy'
        ELSE N'Trong'
    END,
    p.NgayBaoTri = CASE WHEN p.SoPhong IN ('403', '105') THEN '2026-10-01' ELSE NULL END,
    p.GhiChuBaoTri = CASE
        WHEN p.SoPhong = '403' THEN N'Thay bóng đèn ngọc và sơn lại tường phòng khách'
        WHEN p.SoPhong = '105' THEN N'Bảo dưỡng điều hòa do chảy nước nhẹ'
        ELSE NULL
    END
FROM Phong p;
GO

/* ---------------------------------------------------------------------------
   10. DỊCH VỤ (mục 13)
   --------------------------------------------------------------------------- */

SET IDENTITY_INSERT DichVu ON;

INSERT INTO DichVu (MaDichVu, TenDichVu, Gia, MoTa, TrangThai)
VALUES
    (1, N'Bữa sáng tại nhà hàng',   180000.00, N'Bộ thực đơn buffet gồm 20 món',              1),
    (2, N'Xe đưa đón sân bay',     350000.00, N'Đón khách trong giờ 05:00 - 22:00',          1),
    (3, N'Giặt ủi nhanh',           80000.00, N'Nhận trong vòng 4 giờ, tính theo kg',       1),
    (4, N'Hồ bơi + phòng xông',    250000.00, N'Mở cửa 06:00 - 21:00, cần đặt lịch trước',   1),
    (5, N'Dịch vụ phòng 24 giờ',   120000.00, N'Gọi đồ ăn và nước uống tại phòng',          1),
    (6, N'Thuê xe điện sân bay',   500000.00, N'Xe điện 4 chỗ, có tài xế đưa đón',           0);

SET IDENTITY_INSERT DichVu OFF;
GO

/* ---------------------------------------------------------------------------
   11. KẾT QUẢ KIỂM TRA
   --------------------------------------------------------------------------- */

PRINT '=== SO LUONG DU LIEU MAU ===';

SELECT 'TaiKhoan'       AS Bang, COUNT(*) AS SoDong FROM TaiKhoan
UNION ALL SELECT 'LoaiPhong',      COUNT(*) FROM LoaiPhong
UNION ALL SELECT 'Phong',          COUNT(*) FROM Phong
UNION ALL SELECT 'KhachHang',      COUNT(*) FROM KhachHang
UNION ALL SELECT 'DatPhong',       COUNT(*) FROM DatPhong
UNION ALL SELECT 'ChiTietDatPhong',COUNT(*) FROM ChiTietDatPhong
UNION ALL SELECT 'DichVu',         COUNT(*) FROM DichVu;

PRINT '';
PRINT '=== TRANG THAI GIAO DICH ===';

SELECT TrangThai, COUNT(*) AS SoDon, SUM(TongTien) AS TongTien
FROM DatPhong
GROUP BY TrangThai
ORDER BY SoDon DESC;

PRINT '';
PRINT '=== TRANG THAI PHONG ===';

SELECT TrangThai, COUNT(*) AS SoPhong
FROM Phong
GROUP BY TrangThai
ORDER BY SoPhong DESC;
GO