using BlackBeam.Shared.Responses;
using BlackBeam.Services.Loyalty.DTOs;
namespace BlackBeam.Services.Loyalty.Services
{
    public interface ILoyaltyService
    {
        Task<ApiResponse<bool>> EarnPointsAsync(EarnPointsDto request);
        Task<ApiResponse<bool>> RedeemPointsAsync(RedeemPointsDto request);
    }
}
