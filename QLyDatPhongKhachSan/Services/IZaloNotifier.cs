using System.Threading;
using System.Threading.Tasks;

namespace QLyDatPhongKhachSan.Services
{
    public interface IZaloNotifier
    {
        bool DangBat { get; }

        /// <summary>Gui tin cho UserId da cau hinh. Loi mang/cau hinh => false, khong nem exception.</summary>
        Task<bool> GuiTinAsync(string noiDung, CancellationToken cancellationToken = default);
    }
}
