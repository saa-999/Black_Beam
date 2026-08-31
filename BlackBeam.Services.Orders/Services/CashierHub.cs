using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BlackBeam.Services.Orders.Services
{
    [Authorize(Roles = "Cashier")]
    public class CashierHub : Hub
    {
        private readonly OrderTrackingManager _tracker;

        public CashierHub(OrderTrackingManager tracker)
        {
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Cashiers");
            await base.OnConnectedAsync();
        }

        public async Task LockOrderForMe(Guid orderId)
        {
            bool success = _tracker.TryClaimOrder(orderId, Context.ConnectionId);

            if (success)
            {
                await Clients.OthersInGroup("Cashiers").SendAsync("OrderLocked", orderId);
            }
            else
            {
                await Clients.Caller.SendAsync("OrderAlreadyTaken", orderId);
            }
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var abandonedOrders = _tracker.ReleaseOrdersByConnectionId(Context.ConnectionId);

            foreach (var orderId in abandonedOrders)
            {
                await Clients.Group("Cashiers").SendAsync("OrderReassigned", orderId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}