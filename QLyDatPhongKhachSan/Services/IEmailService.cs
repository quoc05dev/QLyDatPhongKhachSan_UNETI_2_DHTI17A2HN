using System.Threading.Tasks;

namespace QLyDatPhongKhachSan.Services
{
    public interface IEmailService
    {
        Task GuiXacNhanDatPhongThanhCongAsync(int maDatPhong);
    }
}
