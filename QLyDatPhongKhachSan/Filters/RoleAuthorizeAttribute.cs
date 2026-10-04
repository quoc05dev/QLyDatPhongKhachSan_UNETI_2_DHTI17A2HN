// Họ và tên: Sinh viên 1
// Mã sinh viên: 23103100091
// Nội dung thực hiện: Filter phân quyền tại Controller theo vai trò trong Session.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QLyDatPhongKhachSan.Helpers;

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

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // Thêm Anti-Cache Headers cho các trang yêu cầu bảo mật đăng nhập
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";

            // 1. Kiểm tra chưa đăng nhập -> Chuyển hướng về Login
            if (!session.IsLoggedIn())
            {
                context.Result = new RedirectToActionResult("Login", "DangNhap", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            // 2. Kiểm tra vai trò nếu có chỉ định vai trò cho phép
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

            base.OnActionExecuting(context);
        }
    }
}
