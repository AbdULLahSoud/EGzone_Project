using EGzone1.Dto.Dashboard;
using System.Threading.Tasks;

namespace EGzone1.Services
{
    public interface ISellerDashboardService
    {
        Task<DashboardResponseDto> GetDashboardDataAsync(int sellerId, string period);
    }
}
