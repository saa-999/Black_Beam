using BlackBeam.Services.Loyalty.Data;
using BlackBeam.Services.Loyalty.DTOs;
using BlackBeam.Services.Loyalty.Model;
using BlackBeam.Services.Loyalty.Enum;
using BlackBeam.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;

namespace BlackBeam.Services.Loyalty.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly LoyalityDbContext _dbContext;
        private readonly IHubContext<CashierHub> _hub;

        public LoyaltyService(LoyalityDbContext dbContext , IHubContext<CashierHub> hub)
        {
            _dbContext = dbContext;
            _hub = hub;
        }

        public async Task<ApiResponse<bool>> EarnPointsAsync(EarnPointsDto request)
        {
            string phoneNumber = request.PhoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(phoneNumber))
                return ApiResponse<bool>.Failure(["رقم الهاتف فارغ"]);

            int pointsToEarn = (int)Math.Floor(request.MonetaryValue * 0.30m);
            if (pointsToEarn <= 0)
                return ApiResponse<bool>.Failure(["المبلغ غير كافي لي اكتساب النقاط"]);

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if(customer == null)
            {
                var req = new ReceiveLoyalty<string>(phoneNumber, "الحساب غير موجود");

                await _hub.Clients.Groups("Cashiers").SendAsync("ReceiveLoyaltNonAccount", req);
                return ApiResponse<bool>.Failure(["بانتظار إنشاء الحساب"]);
            }
            customer.TotalPoints += pointsToEarn;
            customer.Tier = EvaluateTier(customer.TotalPoints);
            _dbContext.Customers.Update(customer);

            var transaction = new LoyaltyTransaction
            {
                PhoneNumber = request.PhoneNumber,
                MonetaryValue = request.MonetaryValue,
                OrderId = request.OrderId,
                Points = pointsToEarn,
                Type = OperationType.Earn
            };

             _dbContext.LoyaltyTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, $"تم إضافة {pointsToEarn} نقطة بنجاح.");
        }
        public async Task<ApiResponse<bool>> RedeemPointsAsync(RedeemPointsDto request)
        {
            string phoneNumber = request.PhoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(phoneNumber))
                return ApiResponse<bool>.Failure(["رقم الجوال  فارغ"]);

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (customer == null)
            {
                var req = new ReceiveLoyalty<string>(phoneNumber, "الحساب غير موجود");
                await _hub.Clients.Groups("Cashiers").SendAsync("ReceiveLoyaltNonAccount", req);
                return ApiResponse<bool>.Failure(["بانتظار إنشاء الحساب"]);
            }

            decimal maxPurchasingPower = customer.TotalPoints * 0.74m;

            if (request.AmountToPay > maxPurchasingPower)
                return ApiResponse<bool>.Failure(["المبلغ غير كافي"]);

            int requiredPoints = (int)Math.Ceiling(request.AmountToPay / 0.74m);

            decimal actualMonetaryValue = requiredPoints * 0.74m;

            decimal remainingChange = actualMonetaryValue - request.AmountToPay;

            int returnedPoints = (int)Math.Floor(remainingChange * 0.30m);

            int netPointsToDeduct = requiredPoints - returnedPoints;

            customer.TotalPoints -= netPointsToDeduct;
            customer.Tier = EvaluateTier(customer.TotalPoints);
            _dbContext.Customers.Update(customer);

            var transaction = new LoyaltyTransaction
            {
                PhoneNumber = request.PhoneNumber,
                Points = -netPointsToDeduct,
                Type = OperationType.Redeem,
                OrderId = request.OrderId,
                MonetaryValue = request.AmountToPay
            };

            _dbContext.LoyaltyTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, $"تم سداد {request.AmountToPay} ريال بخصم {netPointsToDeduct} نقطة.");
        }

        private Tier EvaluateTier(int totalPoints) => totalPoints switch
        {
            >= 50 => Tier.Gold,
            >= 25 => Tier.Silver,
            _ => Tier.Bronze
        };
    }
}
