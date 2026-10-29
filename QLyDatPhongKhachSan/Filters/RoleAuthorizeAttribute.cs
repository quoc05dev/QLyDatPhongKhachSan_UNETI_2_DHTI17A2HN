// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Filter phân quyền tại Controller theo vai trò trong Session.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using QLyDatPhongKhachSan.Helpers;
using QLyDatPhongKhachSan.Repositories.Interfaces;

namespace QLyDatPhongKhachSan.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var session = context.HttpContext.Session;

            // 1. Kiểm tra chưa đăng nhập -> Chuyển hướng về Login
            if (!session.IsLoggedIn())
            {
                context.Result = new RedirectToActionResult("Login", "DangNhap", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            // 2. Đối chiếu lại với DB: tài khoản có thể đã bị xóa, bị khóa hoặc
            //    đổi vai trò sau khi đăng nhập. Không tin tuyệt đối session cũ.
            var maTaiKhoan = session.GetMaTaiKhoan();
            var taiKhoanRepository = context.HttpContext.RequestServices.GetService<ITaiKhoanRepository>();
            if (maTaiKhoan.HasValue && taiKhoanRepository != null)
            {
                var taiKhoan = await taiKhoanRepository.LayTheoMaAsync(maTaiKhoan.Value);
                if (taiKhoan == null || !taiKhoan.TrangThai)
                {
                    session.ClearUserSession();
                    context.Result = new RedirectToActionResult("Login", "DangNhap", new { returnUrl = context.HttpContext.Request.Path });
                    return;
                }

                session.SetUserSession(taiKhoan.MaTaiKhoan, taiKhoan.TenDangNhap, taiKhoan.HoTen, taiKhoan.VaiTro);
            }

            // 3. Kiểm tra vai trò nếu có chỉ định vai trò cho phép
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                var userRole = session.GetVaiTro();
                if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                {
                    // Không có quyền -> Chuyển hướng sang trang AccessDenied
                    context.Result = new RedirectToActionResult("AccessDenied", "DangNhap", null);
                    return;
                }
            }

            await next();
        }
    }
}
