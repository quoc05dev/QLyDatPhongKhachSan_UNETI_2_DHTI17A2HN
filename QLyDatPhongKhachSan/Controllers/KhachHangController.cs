// ==========================================
// Họ và tên: Lê Anh Tuấn
// Mã sinh viên: 23103100086
// Nội dung thực hiện: Module 3 - Quản lý khách hàng, hồ sơ cá nhân
// Chức năng: Khách hàng chỉ xem/sửa hồ sơ của chính mình & Chống truy cập trái phép qua URL
// ==========================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly AppDbContext _context;

        public KhachHangController(AppDbContext context)
        {
            _context = context;
        }

        #region Helper Kiểm tra quyền & Session

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("MaTaiKhoan");
        private string? GetCurrentUserRole() => HttpContext.Session.GetString("VaiTro");
        private int? GetCurrentKhachHangId() => HttpContext.Session.GetInt32("MaKhachHang");

        #endregion

        // =====================================================================
        // CHỨC NĂNG CHÍNH CỦA MODULE 3 (SV3):
        // HỒ SƠ CÁ NHÂN — KHÁCH HÀNG CHỈ XEM/SỬA HỒ SƠ CỦA CHÍNH MÌNH
        // =====================================================================

        // GET: KhachHang/Profile (Xem hồ sơ cá nhân của người đang đăng nhập)
        public async Task<IActionResult> Profile()
        {
            var maTaiKhoan = GetCurrentUserId();
            if (maTaiKhoan == null)
            {
                // Chưa đăng nhập -> Chuyển về trang đăng nhập và lưu lại returnUrl
                return RedirectToAction("Login", "DangNhap", new { returnUrl = "/KhachHang/Profile" });
            }

            // Lấy thông tin tài khoản kèm hồ sơ khách hàng bằng LINQ qua EF Core
            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan.Value);

            if (taiKhoan == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "DangNhap");
            }

            // Nếu tài khoản khách hàng chưa có bản ghi trong bảng KhachHang -> Khởi tạo tự động
            var khachHang = taiKhoan.KhachHang;
            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    HoTen = taiKhoan.HoTen,
                    Email = taiKhoan.Email,
                    SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty,
                    CCCD = string.Empty,
                    QuocTich = "Việt Nam",
                    TrangThai = true
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();

                // Cập nhật lại MaKhachHang vào Session
                HttpContext.Session.SetInt32("MaKhachHang", khachHang.MaKhachHang);
            }

            // Map dữ liệu sang ViewModel để hiển thị trên Razor View
            var viewModel = new HoSoCaNhanViewModel
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TenDangNhap = taiKhoan.TenDangNhap,
                VaiTro = taiKhoan.VaiTro,
                NgayTaoTaiKhoan = taiKhoan.NgayTao,
                HoTen = khachHang.HoTen,
                NgaySinh = khachHang.NgaySinh,
                GioiTinh = khachHang.GioiTinh ?? "Nam",
                CCCD = khachHang.CCCD,
                SoDienThoai = khachHang.SoDienThoai,
                Email = khachHang.Email,
                DiaChi = khachHang.DiaChi,
                QuocTich = khachHang.QuocTich,
                GhiChu = khachHang.GhiChu
            };

            return View(viewModel);
        }

        // POST: KhachHang/Profile (Cập nhật hồ sơ cá nhân của chính mình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(HoSoCaNhanViewModel model)
        {
            var maTaiKhoan = GetCurrentUserId();
            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            // BẢO MẬT: Luôn lấy khách hàng theo MaTaiKhoan trong Session, không tin cậy dữ liệu ID gửi lên từ form
            var khachHang = await _context.KhachHangs
                .Include(kh => kh.TaiKhoan)
                .FirstOrDefaultAsync(kh => kh.MaTaiKhoan == maTaiKhoan.Value);

            if (khachHang == null)
            {
                return NotFound("Không tìm thấy thông tin hồ sơ khách hàng.");
            }

            // Kiểm tra tính hợp lệ nghiệp vụ bổ sung (Mục 7.1 & 13)
            // 1. Ngày sinh không được lớn hơn ngày hiện tại
            if (model.NgaySinh.HasValue && model.NgaySinh.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ (không được lớn hơn ngày hiện tại).");
            }

            // 2. Kiểm tra CCCD không trùng với khách hàng khác trong CSDL
            if (!string.IsNullOrWhiteSpace(model.CCCD))
            {
                bool cccdBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.CCCD == model.CCCD.Trim() && kh.MaKhachHang != khachHang.MaKhachHang);

                if (cccdBiTrung)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD/Hộ chiếu này đã được sử dụng bởi khách hàng khác.");
                }
            }

            // 3. Kiểm tra Email không trùng với khách hàng khác (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bool emailBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.Email == model.Email.Trim() && kh.MaKhachHang != khachHang.MaKhachHang);

                if (emailBiTrung)
                {
                    ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký bởi khách hàng khác.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Điền lại các thông tin chỉ đọc
                model.TenDangNhap = khachHang.TaiKhoan?.TenDangNhap ?? string.Empty;
                model.VaiTro = khachHang.TaiKhoan?.VaiTro ?? "KhachHang";
                model.NgayTaoTaiKhoan = khachHang.TaiKhoan?.NgayTao;
                model.MaKhachHang = khachHang.MaKhachHang;
                model.MaTaiKhoan = khachHang.MaTaiKhoan;
                return View(model);
            }

            // Cập nhật thông tin khách hàng
            khachHang.HoTen = model.HoTen.Trim();
            khachHang.NgaySinh = model.NgaySinh;
            khachHang.GioiTinh = model.GioiTinh;
            khachHang.CCCD = model.CCCD.Trim();
            khachHang.SoDienThoai = model.SoDienThoai.Trim();
            khachHang.Email = model.Email?.Trim();
            khachHang.DiaChi = model.DiaChi?.Trim();
            khachHang.QuocTich = string.IsNullOrWhiteSpace(model.QuocTich) ? "Việt Nam" : model.QuocTich.Trim();
            khachHang.GhiChu = model.GhiChu?.Trim();

            // Đồng bộ sang thông tin Tài Khoản tương ứng
            if (khachHang.TaiKhoan != null)
            {
                khachHang.TaiKhoan.HoTen = khachHang.HoTen;
                khachHang.TaiKhoan.Email = khachHang.Email;
                khachHang.TaiKhoan.SoDienThoai = khachHang.SoDienThoai;
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại họ tên hiển thị trong Session
            HttpContext.Session.SetString("HoTen", khachHang.HoTen);

            TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // =====================================================================
        // BẢO MẬT URL (Mục 7.5 & 22.7):
        // KHÁCH HÀNG KHÔNG ĐƯỢC PHÉP TRUY CẬP HỒ SƠ NGƯỜI KHÁC QUA URL
        // =====================================================================

        // GET: KhachHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var maTaiKhoan = GetCurrentUserId();
            var vaiTro = GetCurrentUserRole();

            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            if (id == null)
            {
                return NotFound();
            }

            // BẢO MẬT: Nếu là Khách hàng -> kiểm tra xem có phải hồ sơ của chính mình không
            if (vaiTro == "KhachHang")
            {
                var myKhachHangId = GetCurrentKhachHangId();
                if (myKhachHangId == null || myKhachHangId.Value != id.Value)
                {
                    // Chặn hành vi cố tình thay đổi id trên URL để xem hồ sơ người khác
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền truy cập hồ sơ của khách hàng có mã ID: {id}. Khách hàng chỉ được xem hồ sơ của chính mình!"
                    });
                }

                // Nếu là của chính mình -> Chuyển về Profile
                return RedirectToAction(nameof(Profile));
            }

            // Admin và Nhân viên được xem chi tiết khách hàng
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                    .ThenInclude(dp => dp.ChiTietDatPhongs)
                        .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(m => m.MaKhachHang == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // =====================================================================
        // CRUD KHÁCH HÀNG DÀNH CHO ADMIN VÀ LỄ TÂN (MODULE 3)
        // =====================================================================

        // GET: KhachHang (Danh sách khách hàng kèm Tìm kiếm, Lọc, Sắp xếp, Phân trang LINQ)
        public async Task<IActionResult> Index(
            string? searchTerm, 
            string? gioiTinhFilter, 
            bool? statusFilter, 
            string? coTaiKhoanFilter, 
            string? sortOrder, 
            int page = 1)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap", new
                {
                    message = "Khách hàng không có quyền xem danh sách khách hàng của hệ thống!"
                });
            }

            if (GetCurrentUserId() == null)
            {
                return RedirectToAction("Login", "DangNhap", new { returnUrl = "/KhachHang" });
            }

            var query = _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                .AsQueryable();

            // Thống kê nhanh toàn bộ CSDL
            int tongKhachHang = await _context.KhachHangs.CountAsync();
            int soKhachHoatDong = await _context.KhachHangs.CountAsync(k => k.TrangThai);
            int soKhachBiKhoa = await _context.KhachHangs.CountAsync(k => !k.TrangThai);
            int soKhachCoTaiKhoan = await _context.KhachHangs.CountAsync(k => k.MaTaiKhoan != null);
            int soKhachVangLai = await _context.KhachHangs.CountAsync(k => k.MaTaiKhoan == null);

            // 1. Tìm kiếm (Họ tên, SĐT, CCCD, Email, Địa chỉ)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(k => k.HoTen.ToLower().Contains(term) ||
                                         k.SoDienThoai.Contains(term) ||
                                         k.CCCD.Contains(term) ||
                                         (k.Email != null && k.Email.ToLower().Contains(term)) ||
                                         (k.DiaChi != null && k.DiaChi.ToLower().Contains(term)));
            }

            // 2. Lọc theo Giới tính
            if (!string.IsNullOrWhiteSpace(gioiTinhFilter))
            {
                query = query.Where(k => k.GioiTinh == gioiTinhFilter);
            }

            // 3. Lọc theo Trạng thái
            if (statusFilter.HasValue)
            {
                query = query.Where(k => k.TrangThai == statusFilter.Value);
            }

            // 4. Lọc theo Loại khách (Có tài khoản / Khách vãng lai)
            if (!string.IsNullOrWhiteSpace(coTaiKhoanFilter))
            {
                if (coTaiKhoanFilter == "co")
                    query = query.Where(k => k.MaTaiKhoan != null);
                else if (coTaiKhoanFilter == "khong")
                    query = query.Where(k => k.MaTaiKhoan == null);
            }

            // 5. Sắp xếp
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "id_desc" : sortOrder;
            query = sortOrder switch
            {
                "id_asc" => query.OrderBy(k => k.MaKhachHang),
                "name_asc" => query.OrderBy(k => k.HoTen),
                "name_desc" => query.OrderByDescending(k => k.HoTen),
                _ => query.OrderByDescending(k => k.MaKhachHang)
            };

            // 6. Phân trang bằng Skip/Take
            int pageSize = 8;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new KhachHangListViewModel
            {
                Items = items,
                SearchTerm = searchTerm,
                GioiTinhFilter = gioiTinhFilter,
                StatusFilter = statusFilter,
                CoTaiKhoanFilter = coTaiKhoanFilter,
                SortOrder = sortOrder,
                TongKhachHang = tongKhachHang,
                SoKhachHoatDong = soKhachHoatDong,
                SoKhachBiKhoa = soKhachBiKhoa,
                SoKhachCoTaiKhoan = soKhachCoTaiKhoan,
                SoKhachVangLai = soKhachVangLai,
                PageIndex = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: KhachHang/Create (Tiếp nhận khách hàng mới / khách vãng lai)
        public async Task<IActionResult> Create()
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            // Nạp danh sách tài khoản khách hàng chưa được liên kết với hồ sơ nào
            ViewBag.TaiKhoans = await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && !_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();

            var model = new KhachHang
            {
                GioiTinh = "Nam",
                QuocTich = "Việt Nam",
                TrangThai = true
            };

            return View(model);
        }

        // POST: KhachHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang khachHang)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            // Kiểm tra nghiệp vụ
            if (khachHang.NgaySinh.HasValue && khachHang.NgaySinh.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ (không được lớn hơn ngày hiện tại).");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.CCCD))
            {
                bool cccdBiTrung = await _context.KhachHangs.AnyAsync(k => k.CCCD == khachHang.CCCD.Trim());
                if (cccdBiTrung)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD/Hộ chiếu này đã được sử dụng bởi khách hàng khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Email))
            {
                bool emailBiTrung = await _context.KhachHangs.AnyAsync(k => k.Email == khachHang.Email.Trim());
                if (emailBiTrung)
                {
                    ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký bởi khách hàng khác.");
                }
            }

            if (ModelState.IsValid)
            {
                khachHang.HoTen = khachHang.HoTen.Trim();
                khachHang.CCCD = khachHang.CCCD.Trim();
                khachHang.SoDienThoai = khachHang.SoDienThoai.Trim();
                khachHang.Email = khachHang.Email?.Trim();
                khachHang.DiaChi = khachHang.DiaChi?.Trim();
                khachHang.QuocTich = string.IsNullOrWhiteSpace(khachHang.QuocTich) ? "Việt Nam" : khachHang.QuocTich.Trim();
                khachHang.GhiChu = khachHang.GhiChu?.Trim();

                _context.Add(khachHang);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm mới khách hàng '{khachHang.HoTen}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TaiKhoans = await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && !_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();

            return View(khachHang);
        }

        // GET: KhachHang/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var maTaiKhoan = GetCurrentUserId();
            var vaiTro = GetCurrentUserRole();

            if (maTaiKhoan == null)
            {
                return RedirectToAction("Login", "DangNhap");
            }

            if (id == null)
            {
                return NotFound();
            }

            // BẢO MẬT: Nếu là Khách hàng -> chỉ được sửa hồ sơ của mình qua trang Profile
            if (vaiTro == "KhachHang")
            {
                var myKhachHangId = GetCurrentKhachHangId();
                if (myKhachHangId == null || myKhachHangId.Value != id.Value)
                {
                    return RedirectToAction("AccessDenied", "DangNhap", new
                    {
                        message = $"Bạn không có quyền chỉnh sửa hồ sơ của khách hàng có mã ID: {id}!"
                    });
                }

                return RedirectToAction(nameof(Profile));
            }

            // Admin / Nhân viên được sửa thông tin khách hàng
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            // Nạp danh sách tài khoản có thể liên kết (chưa liên kết hoặc đang thuộc khách hàng này)
            ViewBag.TaiKhoans = await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && (!_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan) || t.MaTaiKhoan == khachHang.MaTaiKhoan))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();

            return View(khachHang);
        }

        // POST: KhachHang/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang khachHang)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap", new
                {
                    message = "Khách hàng vui lòng cập nhật thông tin tại trang Hồ sơ cá nhân của mình!"
                });
            }

            if (id != khachHang.MaKhachHang)
            {
                return NotFound();
            }

            // Kiểm tra nghiệp vụ
            if (khachHang.NgaySinh.HasValue && khachHang.NgaySinh.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ (không được lớn hơn ngày hiện tại).");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.CCCD))
            {
                bool cccdBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.CCCD == khachHang.CCCD.Trim() && kh.MaKhachHang != id);
                if (cccdBiTrung)
                {
                    ModelState.AddModelError("CCCD", "Số CCCD/Hộ chiếu này đã được sử dụng bởi khách hàng khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Email))
            {
                bool emailBiTrung = await _context.KhachHangs
                    .AnyAsync(kh => kh.Email == khachHang.Email.Trim() && kh.MaKhachHang != id);
                if (emailBiTrung)
                {
                    ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký bởi khách hàng khác.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.KhachHangs
                        .Include(k => k.TaiKhoan)
                        .FirstOrDefaultAsync(k => k.MaKhachHang == id);

                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.HoTen = khachHang.HoTen.Trim();
                    existing.NgaySinh = khachHang.NgaySinh;
                    existing.GioiTinh = khachHang.GioiTinh;
                    existing.CCCD = khachHang.CCCD.Trim();
                    existing.SoDienThoai = khachHang.SoDienThoai.Trim();
                    existing.Email = khachHang.Email?.Trim();
                    existing.DiaChi = khachHang.DiaChi?.Trim();
                    existing.QuocTich = string.IsNullOrWhiteSpace(khachHang.QuocTich) ? "Việt Nam" : khachHang.QuocTich.Trim();
                    existing.TrangThai = khachHang.TrangThai;
                    existing.GhiChu = khachHang.GhiChu?.Trim();
                    existing.MaTaiKhoan = khachHang.MaTaiKhoan;

                    // Đồng bộ sang thông tin Tài Khoản nếu có liên kết
                    if (existing.TaiKhoan != null)
                    {
                        existing.TaiKhoan.HoTen = existing.HoTen;
                        existing.TaiKhoan.Email = existing.Email;
                        existing.TaiKhoan.SoDienThoai = existing.SoDienThoai;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Cập nhật thông tin khách hàng '{existing.HoTen}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.KhachHangs.AnyAsync(e => e.MaKhachHang == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.TaiKhoans = await _context.TaiKhoans
                .Where(t => t.VaiTro == "KhachHang" && (!_context.KhachHangs.Any(kh => kh.MaTaiKhoan == t.MaTaiKhoan) || t.MaTaiKhoan == khachHang.MaTaiKhoan))
                .OrderBy(t => t.TenDangNhap)
                .ToListAsync();

            return View(khachHang);
        }

        // GET: KhachHang/Delete/5 (Xác nhận xóa hoặc khóa khách hàng)
        public async Task<IActionResult> Delete(int? id)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            if (id == null) return NotFound();

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.DatPhongs)
                .FirstOrDefaultAsync(m => m.MaKhachHang == id);

            if (khachHang == null) return NotFound();

            return View(khachHang);
        }

        // POST: KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro == "KhachHang")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            var khachHang = await _context.KhachHangs
                .Include(k => k.DatPhongs)
                .FirstOrDefaultAsync(m => m.MaKhachHang == id);

            if (khachHang == null) return NotFound();

            // Nếu khách hàng đã phát sinh đơn đặt phòng -> Khóa thay vì xóa cứng để đảm bảo toàn vẹn dữ liệu
            if (khachHang.DatPhongs.Any())
            {
                khachHang.TrangThai = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Khách hàng '{khachHang.HoTen}' đã phát sinh {khachHang.DatPhongs.Count} đơn đặt phòng, hệ thống đã chuyển sang trạng thái Bị khóa / Vô hiệu hóa để bảo tồn lịch sử giao dịch!";
                return RedirectToAction(nameof(Index));
            }

            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa khách hàng '{khachHang.HoTen}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: KhachHang/ToggleStatus/5 (Chuyển đổi nhanh trạng thái Hoạt động / Khóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var vaiTro = GetCurrentUserRole();
            if (vaiTro != "Admin" && vaiTro != "NhanVien")
            {
                return RedirectToAction("AccessDenied", "DangNhap");
            }

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null) return NotFound();

            khachHang.TrangThai = !khachHang.TrangThai;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã {(khachHang.TrangThai ? "kích hoạt" : "khóa")} khách hàng '{khachHang.HoTen}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
