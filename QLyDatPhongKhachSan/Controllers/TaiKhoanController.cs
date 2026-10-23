// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Module 1 - Controller Quản lý Tài khoản (CRUD, Tìm kiếm, Lọc, Phân trang LINQ, Khóa/Mở khóa) cho Admin.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Filters;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Models;
using QLyDatPhongKhachSan.Repositories.Interfaces;
using QLyDatPhongKhachSan.ViewModels;

namespace QLyDatPhongKhachSan.Controllers
{
    [RoleAuthorize("Admin")]
    public class TaiKhoanController : Controller
    {
        private readonly ITaiKhoanRepository _taiKhoanRepository;
        private readonly IKhachHangRepository _khachHangRepository;

        public TaiKhoanController(ITaiKhoanRepository taiKhoanRepository, IKhachHangRepository khachHangRepository)
        {
            _taiKhoanRepository = taiKhoanRepository;
            _khachHangRepository = khachHangRepository;
        }

        // GET: TaiKhoan
        public async Task<IActionResult> Index(string searchTerm, string roleFilter, bool? statusFilter, int page = 1)
        {
            int pageSize = 5; // Số bản ghi mỗi trang theo quy định phân trang

            // 4. Phân trang bằng Skip() và Take() trên LINQ EF Core
            int totalItems = await _taiKhoanRepository.DemAsync(searchTerm, roleFilter, statusFilter);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await _taiKhoanRepository.LayPhanTrangAsync(searchTerm, roleFilter, statusFilter, (page - 1) * pageSize, pageSize);

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

            var taiKhoan = await _taiKhoanRepository.LayTheoMaCoKhachHangAsync(id.Value);

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
            if (await _taiKhoanRepository.TenDangNhapDaTonTaiAsync(taiKhoan.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống.");
                return View(taiKhoan);
            }

            // Kiểm tra trùng Email bằng LINQ
            if (await _taiKhoanRepository.EmailDaTonTaiAsync(taiKhoan.Email))
            {
                ModelState.AddModelError("Email", "Email đã được đăng ký bởi tài khoản khác.");
                return View(taiKhoan);
            }

            taiKhoan.NgayTao = DateTime.Now;
            taiKhoan.MatKhau = MatKhauHelper.Hash(taiKhoan.MatKhau);
            await _taiKhoanRepository.ThemAsync(taiKhoan);

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
                await _khachHangRepository.ThemAsync(khachHang);
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

            var taiKhoan = await _taiKhoanRepository.FindAsync(id);
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

            var taiKhoanCu = await _taiKhoanRepository.FindAsync(id);
            if (taiKhoanCu == null)
            {
                return NotFound();
            }

            // Kiểm tra trùng Tên Đăng Nhập ngoại trừ tài khoản hiện tại
            if (await _taiKhoanRepository.TenDangNhapDaTonTaiAsync(taiKhoan.TenDangNhap, id))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã bị trùng với tài khoản khác.");
                return View(taiKhoan);
            }

            // Kiểm tra trùng Email ngoại trừ tài khoản hiện tại
            if (await _taiKhoanRepository.EmailDaTonTaiAsync(taiKhoan.Email, id))
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

                await _taiKhoanRepository.CapNhatAsync(taiKhoanCu);

                // Cập nhật thông tin KhachHang liên kết nếu có
                var khachHang = await _khachHangRepository.LayTheoMaTaiKhoanAsync(id);
                if (khachHang != null)
                {
                    khachHang.HoTen = taiKhoan.HoTen;
                    khachHang.Email = taiKhoan.Email;
                    khachHang.SoDienThoai = taiKhoan.SoDienThoai ?? string.Empty;
                    await _khachHangRepository.CapNhatAsync(khachHang);
                }

                TempData["SuccessMessage"] = doiMatKhau
                    ? $"Cập nhật tài khoản '{taiKhoan.TenDangNhap}' và đổi mật khẩu thành công!"
                    : $"Cập nhật tài khoản '{taiKhoan.TenDangNhap}' thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _taiKhoanRepository.TonTaiAsync(id))
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

            var taiKhoan = await _taiKhoanRepository.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _taiKhoanRepository.CapNhatAsync(taiKhoan);

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

            var taiKhoan = await _taiKhoanRepository.LayTheoMaCoKhachHangAsync(id.Value);

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

            var taiKhoan = await _taiKhoanRepository.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Kiểm tra ràng buộc nghiệp vụ: Không xóa nếu đã có lịch sử liên kết quan trọng
            var khachHang = await _khachHangRepository.LayTheoMaTaiKhoanCoDatPhongAsync(id);
            if (khachHang != null && khachHang.DatPhongs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa tài khoản '{taiKhoan.TenDangNhap}' vì đã có lịch sử giao dịch đặt phòng!";
                return RedirectToAction(nameof(Index));
            }

            if (khachHang != null)
            {
                await _khachHangRepository.XoaAsync(khachHang);
            }

            await _taiKhoanRepository.XoaAsync(taiKhoan);

            TempData["SuccessMessage"] = $"Đã xóa tài khoản '{taiKhoan.TenDangNhap}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
