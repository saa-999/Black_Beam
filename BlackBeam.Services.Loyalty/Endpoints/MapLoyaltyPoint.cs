using BlackBeam.Services.Loyalty.DTOs;
using BlackBeam.Services.Loyalty.Services;
using BlackBeam.Shared.Responses;

namespace BlackBeam.Services.Loyalty.Endpoints
{
    public static class MapLoyaltyPoint
    {
        public static void MapLoyaltyPointEndpoints(this WebApplication app)
        {
            var loyaltyGroup = app.MapGroup("/api/loyalty");

            loyaltyGroup.MapPost("/Earn", async (EarnPointsDto request, ILoyaltyService loyaltyService) =>
            {
                var result = await loyaltyService.EarnPointsAsync(request);
                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            }).RequireAuthorization("CashierOrAdmin");

            loyaltyGroup.MapPost("/Redeem", async (RedeemPointsDto request, ILoyaltyService loyaltyService) =>
            {
                var result = await loyaltyService.RedeemPointsAsync(request);
                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            }).RequireAuthorization("CashierOrAdmin");

            loyaltyGroup.MapGet("/points/{phoneNumber}", async (string phoneNumber, ILoyaltyService loyaltyService) =>
            {
                var result = await loyaltyService.GetLoyaltyPointAndTier(phoneNumber);
                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.NotFound(result);
            }).RequireAuthorization();
        }
    }
}
