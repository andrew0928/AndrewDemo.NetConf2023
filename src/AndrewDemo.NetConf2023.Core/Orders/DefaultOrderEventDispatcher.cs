using System;
using AndrewDemo.NetConf2023.Abstract.Orders;

namespace AndrewDemo.NetConf2023.Core.Orders
{
    public sealed class DefaultOrderEventDispatcher : IOrderEventDispatcher
    {
        public const string DispatcherId = "default-order-event-dispatcher";

        public void Dispatch(OrderCompletedEvent orderEvent)
        {
            ArgumentNullException.ThrowIfNull(orderEvent);
        }

        public void Dispatch(OrderCancelledEvent orderEvent)
        {
            ArgumentNullException.ThrowIfNull(orderEvent);
        }
    }
}
