// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Controller Quản lý Tài khoản (CRUD, Tìm kiếm, Lọc, Phân trang LINQ, Khóa/Mở khóa) cho Admin.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin")]
    public class TaiKhoanController : Controller
    {
        private readonly AppDbContext _context;

        public TaiKhoanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TaiKhoan
        public async Task<IActionResult> Index(string searchTerm, string roleFilter, bool? statusFilter, int page = 1)
        {
            int pageSize = 5; // Số bản ghi mỗi trang theo quy định phân trang
            var query = _context.TaiKhoans.AsQueryable();

            // 1. Tìm kiếm theo Tên đăng nhập, Họ tên hoặc Email
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(term) ||
                                         t.HoTen.ToLower().Contains(term) ||
                                         (t.Email != null && t.Email.ToLower().Contains(term)));
            }

            // 2. Lọc theo Vai Trò (Admin, NhanVien, KhachHang)
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(t => t.VaiTro == roleFilter);
            }

            // 3. Lọc theo Trạng Thái (true: Hoạt động, false: Bị khóa)
            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.TrangThai == statusFilter.Value);
            }

            // Sắp xếp giảm dần theo ngày tạo hoặc mã tài khoản
            query = query.OrderByDescending(t => t.MaTaiKhoan);

            // 4. Phân trang bằng Skip() và Take() trên LINQ EF Core
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new TaiKhoanListViewModel
            {
                Items = items,
                SearchTerm = searchTerm ?? string.Empty,
                RoleFilter = roleFilter ?? string.Empty,
                StatusFilter = statusFilter,
                PageIndex = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: TaiKhoan/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == id);

            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // GET: TaiKhoan/Create
        public IActionResult Create()
        {
            return View(new TaiKhoan());
        }

        // POST: TaiKhoan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDangNhap,MatKhau,HoTen,Email,SoDienThoai,VaiTro,TrangThai")] TaiKhoan taiKhoan)
        {
            // Khi tạo mới, mật khẩu là bắt buộc (không áp dụng quy tắc "để trống = giữ mật khẩu cũ")
            if (string.IsNullOrWhiteSpace(taiKhoan.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống.");
            }
            else if (taiKhoan.MatKhau.Length < 6)
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu ít nhất 6 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                return View(taiKhoan);
            }

            // Kiểm tra trùng Tên Đăng Nhập bằng LINQ
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == taiKhoan.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống.");
                return View(taiKhoan);
            }

            // Kiểm tra trùng Email bằng LINQ
            if (await _context.TaiKhoans.AnyAsync(t => t.Email == taiKhoan.Email))
            {
                ModelState.AddModelError("Email", "Email đã được đăng ký bởi tài khoản khác.");
                return View(taiKhoan);
            }

            taiKhoan.NgayTao = DateTime.Now;
            taiKhoan.MatKhau = MatKhauHelper.Hash(taiKhoan.MatKhau);
            _context.Add(taiKhoan);
            await _context.SaveChangesAsync();

            // Nếu vai trò là Khách hàng, tự động tạo hồ sơ KhachHang tương ứng
            if (taiKhoan.VaiTro == "KhachHang")
            {
                var khachHang = new KhachHang
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    HoTen = taiKhoan.HoTen,
                    Email = taiKhoan.Email,
                    SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty,
                    CCCD = string.Empty,
                    TrangThai = true,
                    GhiChu = "Tạo tự động bởi Admin"
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Thêm mới tài khoản '{taiKhoan.TenDangNhap}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: TaiKhoan/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // POST: TaiKhoan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaTaiKhoan,TenDangNhap,MatKhau,HoTen,Email,SoDienThoai,VaiTro,TrangThai,NgayTao")] TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.MaTaiKhoan)
            {
                return NotFound();
            }

            // Nếu không nhập mật khẩu mới thì coi như giữ mật khẩu cũ.
            bool doiMatKhau = !string.IsNullOrWhiteSpace(taiKhoan.MatKhau);
            if (doiMatKhau)
            {
                if (taiKhoan.MatKhau.Length < 6)
                {
                    ModelState.AddModelError("MatKhau", "Mật khẩu mới phải có ít nhất 6 ký tự.");
                }
            }
            else
            {
                // Ô mật khẩu gửi lên rỗng sẽ bị model binder đổi thành null, khiến
                // framework tự sinh lỗi [Required] và chặn lưu. Để trống ở đây
                // có nghĩa là không đổi mật khẩu nên phải bỏ lỗi này đi.
                ModelState.Remove("MatKhau");
                taiKhoan.MatKhau = string.Empty;
            }

            if (!ModelState.IsValid)
            {
                return View(taiKhoan);
            }

            var taiKhoanCu = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoanCu == null)
            {
                return NotFound();
            }

            // Kiểm tra trùng Tên Đăng Nhập ngoại trừ tài khoản hiện tại
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == taiKhoan.TenDangNhap && t.MaTaiKhoan != id))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã bị trùng với tài khoản khác.");
                return View(taiKhoan);
            }

            // Kiểm tra trùng Email ngoại trừ tài khoản hiện tại
            if (await _context.TaiKhoans.AnyAsync(t => t.Email == taiKhoan.Email && t.MaTaiKhoan != id))
            {
                ModelState.AddModelError("Email", "Email đã bị trùng với tài khoản khác.");
                return View(taiKhoan);
            }

            try
            {
                // Cập nhật từng trường để không ghi đè mật khẩu khi người dùng để trống
                taiKhoanCu.TenDangNhap = taiKhoan.TenDangNhap;
                taiKhoanCu.HoTen = taiKhoan.HoTen;
                taiKhoanCu.Email = taiKhoan.Email;
                taiKhoanCu.SoDienThoai = taiKhoan.SoDienThoai;
                taiKhoanCu.VaiTro = taiKhoan.VaiTro;
                taiKhoanCu.TrangThai = taiKhoan.TrangThai;

                if (doiMatKhau)
                {
                    taiKhoanCu.MatKhau = MatKhauHelper.Hash(taiKhoan.MatKhau);
                }

                await _context.SaveChangesAsync();

                // Cập nhật thông tin KhachHang liên kết nếu có
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTaiKhoan == id);
                if (khachHang != null)
                {
                    khachHang.HoTen = taiKhoan.HoTen;
                    khachHang.Email = taiKhoan.Email;
                    khachHang.SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty;
                    _context.Update(khachHang);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = doiMatKhau
                    ? $"Cập nhật tài khoản '{taiKhoan.TenDangNhap}' và đổi mật khẩu thành công!"
                    : $"Cập nhật tài khoản '{taiKhoan.TenDangNhap}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TaiKhoans.AnyAsync(e => e.MaTaiKhoan == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: TaiKhoan/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUserId = HttpContext.Session.GetMaTaiKhoan();
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            _context.Update(taiKhoan);
            await _context.SaveChangesAsync();

            string statusText = taiKhoan.TrangThai ? "mở khóa" : "khóa";
            TempData["SuccessMessage"] = $"Đã {statusText} tài khoản '{taiKhoan.TenDangNhap}' thành công!";

            return RedirectToAction(nameof(Index));
        }

        // GET: TaiKhoan/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == id);

            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // POST: TaiKhoan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = HttpContext.Session.GetMaTaiKhoan();
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Kiểm tra ràng buộc nghiệp vụ: Không xóa nếu đã có lịch sử liên kết quan trọng
            var khachHang = await _context.KhachHangs.Include(k => k.DatPhongs).FirstOrDefaultAsync(k => k.MaTaiKhoan == id);
            if (khachHang != null && khachHang.DatPhongs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa tài khoản '{taiKhoan.TenDangNhap}' vì đã có lịch sử giao dịch đặt phòng!";
                return RedirectToAction(nameof(Index));
            }

            if (khachHang != null)
            {
                _context.KhachHangs.Remove(khachHang);
            }

            _context.TaiKhoans.Remove(taiKhoan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa tài khoản '{taiKhoan.TenDangNhap}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
