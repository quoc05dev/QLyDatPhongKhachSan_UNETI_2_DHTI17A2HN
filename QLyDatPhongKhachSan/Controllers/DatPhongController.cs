// ==========================================
// Họ và tên: Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - Đặt phòng, kiểm tra điều kiện LINQ, theo dõi lịch sử và hủy đặt phòng
// ==========================================

using Microsoft.AspNetCore.Mvc;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("KhachHang")]
    public class DatPhongController : Controller
    {
        private readonly IDatPhongRepository _datPhongRepository;
        private readonly IChiTietDatPhongRepository _chiTietDatPhongRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly ILoaiPhongRepository _loaiPhongRepository;
        private readonly IKhachHangRepository _khachHangRepository;
        private readonly ITaiKhoanRepository _taiKhoanRepository;
        private readonly IWebHostEnvironment _env;

        public DatPhongController(
            IDatPhongRepository datPhongRepository,
            IChiTietDatPhongRepository chiTietDatPhongRepository,
            IPhongRepository phongRepository,
            ILoaiPhongRepository loaiPhongRepository,
            IKhachHangRepository khachHangRepository,
            ITaiKhoanRepository taiKhoanRepository,
            IWebHostEnvironment env)
        {
            _datPhongRepository = datPhongRepository;
            _chiTietDatPhongRepository = chiTietDatPhongRepository;
            _phongRepository = phongRepository;
            _loaiPhongRepository = loaiPhongRepository;
            _khachHangRepository = khachHangRepository;
            _taiKhoanRepository = taiKhoanRepository;
            _env = env;
        }

        #region Helper: Quản lý Session & Khách hàng

        private int? GetCurrentMaTaiKhoan() => HttpContext.Session.GetMaTaiKhoan();
        private string GetCurrentVaiTro() => HttpContext.Session.GetVaiTro();

        /// <summary>
        /// Lấy hoặc tự động khởi tạo hồ sơ KhachHang tương ứng với tài khoản đang đăng nhập
        /// </summary>
        private async Task<KhachHang?> GetOrCreateCurrentKhachHangAsync()
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null) return null;

            var taiKhoan = await _taiKhoanRepository.LayTheoMaCoKhachHangTheoDoiAsync(maTaiKhoan.Value);

            if (taiKhoan == null) return null;

            var khachHang = taiKhoan.KhachHang;
            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    HoTen = taiKhoan.HoTen,
                    Email = taiKhoan.Email,
                    SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty,
                    CCCD = "000000000000", // Giá trị mặc định hợp lệ tránh lỗi validation [Required]
                    QuocTich = "Việt Nam",
                    TrangThai = true
                };
                await _khachHangRepository.ThemAsync(khachHang);
            }

            return khachHang;
        }

        #endregion

        // =====================================================================
        // 1. TẠO ĐẶT PHÒNG (BOOK) — KIỂM TRA ĐIỀU KIỆN NGHIỆP VỤ & TRÙNG LỊCH LINQ
        // =====================================================================

        // GET: DatPhong/Book?maPhong=5
        public async Task<IActionResult> Book(int? maPhong)
        {
            // 1. Kiểm tra đăng nhập
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Book", "DatPhong", new { maPhong }) });
            }

            // 2. Kiểm tra mã phòng: Nếu chưa chọn phòng, chuyển hướng về trang Danh sách phòng đặt phòng (Module 3)
            if (maPhong == null)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn phòng bạn muốn đặt từ danh sách phòng bên dưới.";
                return RedirectToAction(nameof(Index));
            }

            // Nạp danh sách các phòng còn trống để khách hàng có thể chọn/đổi phòng ngay trên trang đặt
            var danhSachPhongTrong = await _phongRepository.LayDanhSachPhongTrongAsync();

            if (!danhSachPhongTrong.Any())
            {
                TempData["ErrorMessage"] = "Hiện tại khách sạn đã hết phòng trống. Quý khách vui lòng quay lại sau.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhSachPhongTrong = danhSachPhongTrong;

            // 3. Truy vấn thông tin phòng và loại phòng
            var phong = await _phongRepository.LayTheoMaAsync(maPhong.Value);

            if (phong == null)
            {
                TempData["ErrorMessage"] = "Phòng yêu cầu không tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            // 4. Kiểm tra trạng thái phòng: chỉ đặt phòng khi phòng ở trạng thái "Trong" (có thể kinh doanh - Đề 7.3)
            if (!string.Equals(phong.TrangThai, "Trong", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện không ở trạng thái trống (trạng thái: {phong.TrangThai}), quý khách vui lòng chọn phòng khác.";
                return RedirectToAction(nameof(Index));
            }

            // 5. Nạp thông tin khách hàng hiện tại
            var khachHang = await GetOrCreateCurrentKhachHangAsync();

            var viewModel = new DatPhongCreateViewModel
            {
                MaPhong = phong.MaPhong,
                SoPhong = phong.SoPhong,
                TenLoaiPhong = phong.LoaiPhong?.TenLoai ?? "Tiêu chuẩn",
                DonGia = phong.DonGia,
                SoNguoiToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2,
                TienNghi = phong.TienNghi,
                HuongPhong = phong.HuongPhong,
                Tang = phong.Tang,
                HoTenKhachHang = khachHang?.HoTen ?? string.Empty,
                SoDienThoai = khachHang?.SoDienThoai ?? string.Empty,
                CCCD = (khachHang?.CCCD == "000000000000") ? string.Empty : (khachHang?.CCCD ?? string.Empty),
                Email = khachHang?.Email,
                NgayNhan = DateTime.Today,
                NgayTraDuKien = DateTime.Today.AddDays(1),
                SoNguoi = 1,
                TienCoc = 0
            };

            return View(viewModel);
        }

        // POST: DatPhong/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(DatPhongCreateViewModel model)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Book", "DatPhong", new { maPhong = model.MaPhong }) });
            }

            var phong = await _phongRepository.LayTheoMaAsync(model.MaPhong);

            if (phong == null)
            {
                TempData["ErrorMessage"] = "Phòng không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            // ==========================================
            // KIỂM TRA 5 ĐIỀU KIỆN NGHIỆP VỤ BẮT BUỘC (Mục 7.3 & 13)
            // ==========================================

            // Điều kiện 1: Trạng thái phòng có thể kinh doanh (phòng phải ở trạng thái "Trong" - Đề 7.3)
            if (!string.Equals(phong.TrangThai, "Trong", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, $"Phòng {phong.SoPhong} hiện không ở trạng thái trống (trạng thái: {phong.TrangThai}), không thể tiếp nhận đặt phòng.");
            }

            // Điều kiện 2: Ngày nhận không được ở trong quá khứ
            if (model.NgayNhan.Date < DateTime.Today)
            {
                ModelState.AddModelError("NgayNhan", "Ngày nhận phòng không được ở trong quá khứ.");
            }

            // Điều kiện 3: Ngày trả phải sau ngày nhận ít nhất 1 ngày
            if (model.NgayTraDuKien.Date <= model.NgayNhan.Date)
            {
                ModelState.AddModelError("NgayTraDuKien", "Ngày trả phòng dự kiến phải sau ngày nhận phòng ít nhất 1 ngày.");
            }

            // Điều kiện 4: Kiểm tra sức chứa tối đa của loại phòng
            int sucChuaToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2;
            if (model.SoNguoi > sucChuaToiDa)
            {
                ModelState.AddModelError("SoNguoi", $"Số lượng khách ({model.SoNguoi} người) vượt quá sức chứa tối đa của phòng ({sucChuaToiDa} người).");
            }

            // Điều kiện 5: KIỂM TRA TRÙNG LỊCH ĐẶT PHÒNG BẰNG LINQ
            // Không cho đặt nếu phòng đã có booking hoạt động trùng khoảng thời gian [NgayNhan, NgayTraDuKien]
            bool biTrungLich = await _chiTietDatPhongRepository.PhongDaDuocDatTrongKhoangAsync(model.MaPhong, model.NgayNhan, model.NgayTraDuKien);

            if (biTrungLich)
            {
                ModelState.AddModelError(string.Empty, $"Phòng {phong.SoPhong} đã có khách đặt trong khoảng thời gian {model.NgayNhan:dd/MM/yyyy} - {model.NgayTraDuKien:dd/MM/yyyy}. Quý khách vui lòng chọn ngày khác hoặc phòng khác.");
            }

            // Nếu không hợp lệ, điền lại thông tin phòng và trả về View
            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachPhongTrong = await _phongRepository.LayDanhSachPhongTrongAsync();

                model.SoPhong = phong.SoPhong;
                model.TenLoaiPhong = phong.LoaiPhong?.TenLoai ?? "Tiêu chuẩn";
                model.DonGia = phong.DonGia;
                model.SoNguoiToiDa = phong.LoaiPhong?.SoNguoiToiDa ?? 2;
                model.TienNghi = phong.TienNghi;
                model.HuongPhong = phong.HuongPhong;
                model.Tang = phong.Tang;
                return View(model);
            }

            // Lấy hồ sơ khách hàng hiện tại
            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // Cập nhật thông tin khách hàng theo dữ liệu khách đã nhập trên form đặt phòng
            if (!string.IsNullOrWhiteSpace(model.HoTenKhachHang))
            {
                khachHang.HoTen = model.HoTenKhachHang.Trim();
            }
            if (!string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                khachHang.SoDienThoai = model.SoDienThoai.Trim();
            }
            if (!string.IsNullOrWhiteSpace(model.CCCD))
            {
                khachHang.CCCD = model.CCCD.Trim();
            }
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                khachHang.Email = model.Email.Trim();
            }

            // Đồng bộ sang tài khoản liên kết để header/hồ sơ hiển thị đúng thông tin mới
            if (khachHang.TaiKhoan != null)
            {
                khachHang.TaiKhoan.HoTen = khachHang.HoTen;
                khachHang.TaiKhoan.Email = khachHang.Email;
                khachHang.TaiKhoan.SoDienThoai = khachHang.SoDienThoai;
            }

            await _khachHangRepository.CapNhatAsync(khachHang);
            HttpContext.Session.SetString("HoTen", khachHang.HoTen);

            // Tính số ngày ở và tổng tiền dự kiến
            int soNgay = (model.NgayTraDuKien.Date - model.NgayNhan.Date).Days;
            if (soNgay < 1) soNgay = 1;
            decimal tongTien = soNgay * phong.DonGia;
            decimal tienCoc = model.TienCoc > tongTien ? tongTien : Math.Max(0, model.TienCoc);

            // 1. Tạo đơn đặt phòng chính (DatPhong)
            var datPhong = new DatPhong
            {
                MaKhachHang = khachHang.MaKhachHang,
                NgayDat = DateTime.Now,
                NgayNhan = model.NgayNhan.Date,
                NgayTraDuKien = model.NgayTraDuKien.Date,
                SoNguoi = model.SoNguoi,
                TienCoc = tienCoc,
                TongTien = tongTien,
                TrangThai = "ChoXuLy", // Luồng trạng thái ban đầu: Chờ xử lý
                GhiChu = model.GhiChu?.Trim()
            };

            await _datPhongRepository.ThemAsync(datPhong);

            // 2. Tạo chi tiết đặt phòng (ChiTietDatPhong) liên kết với phòng đã chọn
            var chiTiet = new ChiTietDatPhong
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaPhong = phong.MaPhong,
                NgayNhan = model.NgayNhan.Date,
                NgayTra = model.NgayTraDuKien.Date,
                DonGia = phong.DonGia,
                SoNgay = soNgay,
                ThanhTien = tongTien,
                TrangThai = "ChoNhan"
            };

            await _chiTietDatPhongRepository.ThemAsync(chiTiet);

            TempData["SuccessMessage"] = $"Đặt phòng {phong.SoPhong} thành công! Đơn đặt phòng #{datPhong.MaDatPhong:D4} đã được gửi tới bộ phận Lễ tân xử lý.";
            return RedirectToAction(nameof(Details), new { id = datPhong.MaDatPhong });
        }

        // =====================================================================
        // 2. MÀN HÌNH ĐẶT PHÒNG — DANH SÁCH PHÒNG & TRẠNG THÁI (MODULE 3)
        // Khách hàng xem danh sách phòng, kiểm tra trạng thái và nhấn đặt phòng
        // =====================================================================

        // GET: DatPhong (hoặc DatPhong/Index)
        public async Task<IActionResult> Index(
            int? maLoaiPhong, 
            int? tang, 
            string? trangThai, 
            decimal? giaMin, 
            decimal? giaMax, 
            string? sortOrder, 
            string? keyword,
            int page = 1,
            int pageSize = 6)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Index", "DatPhong") });
            }

            // Thống kê nhanh toàn bộ số lượng phòng theo trạng thái
            var allPhongs = await _phongRepository.LayTatCaAsync();
            int tongSoPhong = allPhongs.Count;
            int soPhongTrong = allPhongs.Count(p => p.TrangThai == "Trong");
            int soPhongDangSuDung = allPhongs.Count(p => p.TrangThai == "DangSuDung");
            int soPhongDangXuLy = allPhongs.Count(p => p.TrangThai == "DangXuLy");
            int soPhongBaoTri = allPhongs.Count(p => p.TrangThai == "BaoTri");

            // 1-6. Tìm kiếm từ khóa, lọc theo loại phòng/tầng/trạng thái/khoảng giá rồi sắp xếp
            var tatCaPhong = await _phongRepository.TimKiemVaLocPhongDatPhongAsync(maLoaiPhong, tang, trangThai, giaMin, giaMax, keyword);

            // 7. Phân tích sở thích khách hàng từ lịch sử đặt phòng (thuật toán gợi ý thông minh)
            int? soThichLoaiPhong = null;
            string? soThichHuong = null;

            var khachHangHienTai = await GetOrCreateCurrentKhachHangAsync();
            if (khachHangHienTai != null)
            {
                var lichSu = await _chiTietDatPhongRepository.LayLichSuTheoKhachHangAsync(khachHangHienTai.MaKhachHang);

                soThichLoaiPhong = lichSu
                    .GroupBy(l => l.Phong!.MaLoaiPhong)
                    .OrderByDescending(g => g.Count())
                    .Select(g => (int?)g.Key)
                    .FirstOrDefault();

                soThichHuong = lichSu
                    .Where(l => !string.IsNullOrWhiteSpace(l.Phong!.HuongPhong))
                    .GroupBy(l => l.Phong!.HuongPhong)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();
            }

            // 8. Tính điểm gợi ý cho từng phòng: ưu tiên phòng trống, khớp sở thích, nhiều tiện nghi
            var danhSachItem = new List<DatPhongPhongItem>();
            foreach (var p in tatCaPhong)
            {
                var item = new DatPhongPhongItem
                {
                    Phong = p,
                    HinhAnh = AnhPhongHelper.LayAnh(p.LoaiPhong),
                    MoTa = AnhPhongHelper.LayMoTa(p.LoaiPhong)
                };

                int diem = 0;
                string? lyDo = null;

                if (item.DangTrong) diem += 1000;

                if (soThichLoaiPhong.HasValue && p.MaLoaiPhong == soThichLoaiPhong.Value)
                {
                    diem += 150;
                    lyDo ??= "Đúng loại phòng bạn thường đặt";
                }

                if (!string.IsNullOrWhiteSpace(soThichHuong)
                    && string.Equals(p.HuongPhong, soThichHuong, StringComparison.OrdinalIgnoreCase))
                {
                    diem += 70;
                    lyDo ??= $"Hướng {p.HuongPhong} bạn ưa thích";
                }

                if (!string.IsNullOrWhiteSpace(p.TienNghi)) diem += 20;
                if ((p.LoaiPhong?.SoNguoiToiDa ?? 0) >= 2) diem += 5;

                item.DiemGoiY = diem;
                item.LyDoGoiY = lyDo;
                danhSachItem.Add(item);
            }

            // 9. Sắp xếp: phòng có thể đặt được lên trước, rồi tới điểm gợi ý, cuối cùng theo lựa chọn của khách
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "goiy" : sortOrder;
            var ordered = danhSachItem
                .OrderByDescending(i => i.DangTrong)
                .ThenByDescending(i => i.DiemGoiY);

            ordered = sortOrder switch
            {
                "price_asc" => ordered.ThenBy(i => i.DonGia),
                "price_desc" => ordered.ThenByDescending(i => i.DonGia),
                "tang_asc" => ordered.ThenBy(i => i.Phong.Tang).ThenBy(i => i.Phong.SoPhong),
                "phong_asc" => ordered.ThenBy(i => i.Phong.SoPhong),
                _ => ordered.ThenBy(i => i.Phong.SoPhong)
            };

            var danhSachXepHang = ordered.ToList();

            // Gắn nhãn "Đề xuất cho bạn" cho tối đa 3 phòng trống hàng đầu
            int soPhongGoiY = 0;
            foreach (var item in danhSachXepHang.Where(x => x.DangTrong))
            {
                if (soPhongGoiY >= 3) break;
                item.LaGoiY = true;
                item.LyDoGoiY ??= "Đang trống, sẵn sàng đặt ngay";
                soPhongGoiY++;
            }

            // 10. Phân trang danh sách phòng (Tối đa 6 phòng / trang)
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            int totalItems = danhSachXepHang.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var danhSachPhong = danhSachXepHang
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var danhSachLoaiPhong = await _loaiPhongRepository.LayTatCaAsync();

            var viewModel = new DatPhongChonPhongViewModel
            {
                DanhSachPhong = danhSachPhong,
                DanhSachLoaiPhong = danhSachLoaiPhong,
                MaLoaiPhong = maLoaiPhong,
                Tang = tang,
                TrangThai = trangThai,
                GiaMin = giaMin,
                GiaMax = giaMax,
                SortOrder = sortOrder,
                Keyword = keyword,
                TongSoPhong = tongSoPhong,
                SoPhongTrong = soPhongTrong,
                SoPhongDangSuDung = soPhongDangSuDung,
                SoPhongDangXuLy = soPhongDangXuLy,
                SoPhongBaoTri = soPhongBaoTri,
                SoPhongGoiY = soPhongGoiY,
                SoThichLoaiPhong = danhSachLoaiPhong.FirstOrDefault(lp => lp.MaLoaiPhong == soThichLoaiPhong)?.TenLoai,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // =====================================================================
        // 3. THEO DÕI ĐẶT PHÒNG (LỊCH SỬ & CHI TIẾT) — KÈM PHÂN TRANG & SẮP XẾP
        // =====================================================================

        // GET: DatPhong/LichSu (Lịch sử đặt phòng của khách hàng hiện tại kèm phân trang & sắp xếp - Đề 22)
        public async Task<IActionResult> LichSu(string? trangThaiFilter, string? sortOrder, int page = 1, int pageSize = 5)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("LichSu", "DatPhong") });
            }

            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // Thống kê nhanh các trạng thái
            var thongKeTrangThai = await _datPhongRepository.ThongKeTrangThaiCuaKhachHangAsync(khachHang.MaKhachHang);
            int tongDon = thongKeTrangThai.Values.Sum();
            int donChoXuLy = thongKeTrangThai.GetValueOrDefault("ChoXuLy");
            int donDaXacNhan = thongKeTrangThai.GetValueOrDefault("DaXacNhan") + thongKeTrangThai.GetValueOrDefault("DangXuLy");
            int donHoanThanh = thongKeTrangThai.GetValueOrDefault("HoanThanh");
            int donDaHuy = thongKeTrangThai.GetValueOrDefault("DaHuy");

            // Phân trang (Paging - Mục 22 - De_15)
            int totalItems = await _datPhongRepository.DemLichSuCuaKhachHangAsync(khachHang.MaKhachHang, trangThaiFilter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // Sắp xếp (Sorting - Mục 22 - De_15)
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "date_desc" : sortOrder;

            var list = await _datPhongRepository.LayLichSuCuaKhachHangAsync(khachHang.MaKhachHang, trangThaiFilter, sortOrder, (page - 1) * pageSize, pageSize);

            var viewModel = new DatPhongLichSuViewModel
            {
                DanhSachDon = list,
                TrangThaiFilter = trangThaiFilter,
                SortOrder = sortOrder,
                TongDon = tongDon,
                DonChoXuLy = donChoXuLy,
                DonDaXacNhan = donDaXacNhan,
                DonHoanThanh = donHoanThanh,
                DonDaHuy = donDaHuy,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View("LichSu", viewModel);
        }

        // GET: DatPhong/Details/5 (Chi tiết đơn đặt phòng — Lọc MaKhachHang ngay trong query)
        public async Task<IActionResult> Details(int? id)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("Details", "DatPhong", new { id }) });
            }

            if (id == null)
            {
                return NotFound();
            }

            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // Lọc MaKhachHang trực tiếp trong query để tối ưu hiệu năng và an toàn dữ liệu
            var datPhong = await _datPhongRepository.LayChiTietCuaKhachHangAsync(id.Value, khachHang.MaKhachHang);

            // BẢO MẬT URL (Mục 7.5 & 22.7): Nếu không tìm thấy, kiểm tra xem đơn có tồn tại ở khách khác không
            if (datPhong == null)
            {
                bool tonTaiDonThuocKhachKhac = await _datPhongRepository.TonTaiAsync(id.Value);
                if (tonTaiDonThuocKhachKhac)
                {
                    TempData["ErrorMessage"] = $"Bạn không có quyền xem thông tin đơn đặt phòng #{id}. Khách hàng chỉ được xem đơn đặt phòng của chính mình!";
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền xem thông tin đơn đặt phòng #{id}. Khách hàng chỉ được xem đơn đặt phòng của chính mình!"
                    });
                }
                return NotFound("Không tìm thấy đơn đặt phòng yêu cầu.");
            }

            var chiTiet = datPhong.ChiTietDatPhongs.FirstOrDefault();
            int soNgay = (datPhong.NgayTraDuKien.Date - datPhong.NgayNhan.Date).Days;
            if (soNgay < 1) soNgay = 1;

            var viewModel = new DatPhongChiTietViewModel
            {
                DatPhong = datPhong,
                ChiTiet = chiTiet,
                Phong = chiTiet?.Phong,
                KhachHang = datPhong.KhachHang,
                SoNgayO = soNgay
            };

            return View(viewModel);
        }

        // =====================================================================
        // 4. XEM CHI TIẾT PHÒNG (ROOM DETAIL) — Khách hàng bấm vào phòng để xem thông tin & ảnh
        // =====================================================================

        // GET: DatPhong/ChiTietPhong/5
        public async Task<IActionResult> ChiTietPhong(int? id)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = Url.Action("ChiTietPhong", "DatPhong", new { id }) });
            }

            if (id == null)
            {
                return NotFound();
            }

            var phong = await _phongRepository.LayTheoMaAsync(id.Value);

            if (phong == null)
            {
                TempData["ErrorMessage"] = "Phòng yêu cầu không tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var model = new DatPhongPhongItem
            {
                Phong = phong,
                HinhAnh = AnhPhongHelper.LayAnh(phong.LoaiPhong),
                DanhSachAnh = AnhPhongHelper.LayDanhSachAnh(phong, _env.WebRootPath),
                MoTa = AnhPhongHelper.LayMoTa(phong.LoaiPhong)
            };

            return View(model);
        }

        // =====================================================================
        // 3. HỦY ĐẶT PHÒNG (CANCEL) — SIẾT GUARD TRẠNG THÁI & GIẢI PHÓNG PHÒNG
        // =====================================================================
        // POST: DatPhong/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var maTaiKhoan = GetCurrentMaTaiKhoan();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            var khachHang = await GetOrCreateCurrentKhachHangAsync();
            if (khachHang == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // Lọc MaKhachHang trực tiếp trong query
            var datPhong = await _datPhongRepository.LayDeHuyCuaKhachHangAsync(id, khachHang.MaKhachHang);

            // BẢO MẬT URL (Mục 7.5 & 22.7): Kiểm tra nếu cố tình gửi request hủy đơn của khách khác
            if (datPhong == null)
            {
                bool tonTaiDonThuocKhachKhac = await _datPhongRepository.TonTaiAsync(id);
                if (tonTaiDonThuocKhachKhac)
                {
                    TempData["ErrorMessage"] = $"Bạn không có quyền thao tác hủy đơn đặt phòng #{id} của người khác!";
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền thao tác hủy đơn đặt phòng #{id} của người khác!"
                    });
                }
                return NotFound("Không tìm thấy đơn đặt phòng.");
            }

            // ==========================================
            // SIẾT CHẶT ĐIỀU KIỆN HỦY (Mục 7.4):
            // Chỉ cho phép hủy khi ở trạng thái "ChoXuLy" hoặc "DangXuLy"
            // Tuyệt đối không cho hủy khi đã nhận phòng ("DangSuDung"), "HoanThanh" hoặc đã xác nhận ("DaXacNhan")
            // ==========================================
            if (datPhong.TrangThai != "ChoXuLy" && datPhong.TrangThai != "DangXuLy")
            {
                TempData["ErrorMessage"] = $"Chỉ có thể hủy đơn đặt phòng ở trạng thái 'Chờ xử lý' hoặc 'Đang xử lý'. Đơn #{datPhong.MaDatPhong:D4} hiện đang ở trạng thái '{datPhong.TrangThai}'.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Cập nhật trạng thái hủy đơn chính
            datPhong.TrangThai = "DaHuy";
            datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu)
                ? $"[Đã hủy bởi khách hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm}]"
                : $"{datPhong.GhiChu} | [Đã hủy bởi khách hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm}]";

            // Cập nhật chi tiết đặt phòng và giải phóng phòng về 'Trong' nếu phòng đang bị giữ chỗ
            foreach (var ct in datPhong.ChiTietDatPhongs)
            {
                ct.TrangThai = "DaHuy";
                if (ct.Phong != null && (ct.Phong.TrangThai == "DaDat" || ct.Phong.TrangThai == "DangXuLy"))
                {
                    ct.Phong.TrangThai = "Trong";
                }
            }

            await _datPhongRepository.CapNhatAsync(datPhong);

            TempData["SuccessMessage"] = $"Đã hủy đơn đặt phòng #{datPhong.MaDatPhong:D4} thành công và phòng đã được giải phóng sẵn sàng đón khách.";
            return RedirectToAction(nameof(LichSu));
        }
    }
}
