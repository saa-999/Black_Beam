using BlackBeam.Services.Loyalty.Data;
using BlackBeam.Services.Loyalty.DTOs;
using BlackBeam.Services.Loyalty.Model;
using BlackBeam.Services.Loyalty.Enum;
using BlackBeam.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Runtime.CompilerServices;

namespace BlackBeam.Services.Loyalty.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly LoyalityDbContext _dbContext;
        private readonly IHubContext<CashierHub> _hub;
        private readonly IHttpContextAccessor _accessor;
        private readonly IHttpClientFactory _clientFactory;

        public LoyaltyService(LoyalityDbContext dbContext, IHubContext<CashierHub> hub, IHttpContextAccessor accessor, IHttpClientFactory clientFactory)
        {
            _dbContext = dbContext;
            _hub = hub;
            _accessor = accessor;
            _clientFactory = clientFactory;
        }

        public async Task<ApiResponse<bool>> EarnPointsAsync(EarnPointsDto request)
        {
            string phoneNumber = request.PhoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(phoneNumber))
                return ApiResponse<bool>.Failure(["رقم الهاتف فارغ"]);

            int pointsToEarn = (int)Math.Floor(request.MonetaryValue * 0.30m);
            if (pointsToEarn <= 0)
                return ApiResponse<bool>.Failure(["المبلغ غير كافي لي اكتساب النقاط"]);

            bool isLogin = await GetPhoneNumber(phoneNumber);
            if (!isLogin)
                return ApiResponse<bool>.Failure(["الحساب غير مسجل او تم تعطيله "]);

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
        public async Task<ApiResponse<PointDto>> GetLoyaltyPointAndTier(string phoneNumber)
        {
            string cleanPhoneNumber = phoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(cleanPhoneNumber))
                return ApiResponse<PointDto>.Failure(["رقم الهاتف يجب ان يكون غير فارغ"]);

            var customer = await _dbContext.Customers.FirstOrDefaultAsync(i => i.PhoneNumber == phoneNumber);

            if (customer == null)
                return ApiResponse<PointDto>.Failure(["لا يوجد حساب"]);

            var infoPoint = new PointDto
            {
                point = customer.TotalPoints,
                tier = customer.Tier
            };
            
            return ApiResponse<PointDto>.Success(infoPoint);
        }
        

        private Tier EvaluateTier(int totalPoints) => totalPoints switch
        {
            >= 50 => Tier.Gold,
            >= 25 => Tier.Silver,
            _ => Tier.Bronze
        };

        private async Task<bool> GetPhoneNumber(string phoneNumber)
        {
            string cleanPhoneNumber = phoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanPhoneNumber))
                return false;

            string? authHeader = _accessor.HttpContext?.Request.Headers.Authorization;

      
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var client = _clientFactory.CreateClient("Identity");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync($"/api/auth/GetPhoneNumber?phoneNumber={cleanPhoneNumber}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}
