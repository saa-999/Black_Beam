using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BlackBeam.Services.Orders.Services
{
    public class OrderTrackingManager
    {
        private readonly ConcurrentDictionary<Guid, string> _claimedOrders = new();

        public bool TryClaimOrder(Guid orderId, string connectionId)
        {
            return _claimedOrders.TryAdd(orderId, connectionId);
        }

        public void ReleaseOrder(Guid orderId)
        {
            _claimedOrders.TryRemove(orderId, out _);
        }

        public Guid[] ReleaseOrdersByConnectionId(string connectionId)
        {
            var ordersToRelease = _claimedOrders
                .Where(x => x.Value == connectionId)
                .Select(x => x.Key)
                .ToArray();

            foreach (var orderId in ordersToRelease)
            {
                _claimedOrders.TryRemove(orderId, out _);
            }

            return ordersToRelease;
        }
    }
}