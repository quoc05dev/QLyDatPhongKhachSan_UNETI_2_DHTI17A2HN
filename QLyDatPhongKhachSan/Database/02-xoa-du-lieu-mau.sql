/* ============================================================================
   ĐỀ TÀI 15 - DỮ LIỆU MẪU
   XÓA SẠCH DỮ LIỆU MẪU, KHÔI PHỤC DATABASE VỀ TRẠNG THÁI RỖNG

   Họ và tên  : Đặng Minh Quốc
   Mã SV      : 23103100069 - Module 5

   MỤC ĐÍCH
   Sau khi dùng dữ liệu mẫu để kiểm chứng Dashboard và báo cáo thống kê,
   chạy script này để đưa database về trạng thái rỗng, sẵn sàng nhập dữ liệu
   thật trước khi trình bày với giảng viên.

   CÁCH CHẠY
     sqlcmd -S "(localdb)\MSSQLLocalDB" -d QLyDatPhongKhachSanDB -f 65001 -i <duong-dan-file>

   LƯU Ý
   Thứ tự DELETE tuân thủ ràng buộc khóa ngoại: ChiTietDatPhong -> DatPhong ->
   KhachHang -> Phong -> LoaiPhong -> TaiKhoan.
   Không đụng tới cấu trúc bảng, mọi thay đổi schema đều nằm trong Migration.
   ============================================================================ */

SET NOCOUNT ON;
-- Bắt buộc bật lại vì sqlcmd mặc định đặt QUOTED_IDENTIFIER ở trạng thái OFF.
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

DELETE FROM ChiTietDatPhong;
DELETE FROM DatPhong;
DELETE FROM KhachHang;
DELETE FROM Phong;
DELETE FROM LoaiPhong;
DELETE FROM TaiKhoan;
DELETE FROM DichVu;

COMMIT TRANSACTION;
GO

PRINT '=== TRANG THAI SAU KHI XOA DU LIEU MAU ===';

SELECT 'TaiKhoan'       AS Bang, COUNT(*) AS SoDong FROM TaiKhoan
UNION ALL SELECT 'LoaiPhong',      COUNT(*) FROM LoaiPhong
UNION ALL SELECT 'Phong',          COUNT(*) FROM Phong
UNION ALL SELECT 'KhachHang',      COUNT(*) FROM KhachHang
UNION ALL SELECT 'DatPhong',       COUNT(*) FROM DatPhong
UNION ALL SELECT 'ChiTietDatPhong',COUNT(*) FROM ChiTietDatPhong
UNION ALL SELECT 'DichVu',         COUNT(*) FROM DichVu;

PRINT '';
PRINT 'Cau truc ban va du lieu mau van duoc giu nguyen. Hay chay 01-du-lieu-mau.sql de nap lai.';
GO