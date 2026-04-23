using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Reservations
{
    public sealed class PetShopOrderEventDispatcher : IOrderEventDispatcher
    {
        private readonly PetShopReservationService _reservationService;

        public PetShopOrderEventDispatcher(PetShopReservationService reservationService)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        }

        public void Dispatch(OrderCompletedEvent orderEvent)
        {
            ArgumentNullException.ThrowIfNull(orderEvent);

            foreach (var line in orderEvent.Lines)
            {
                _reservationService.ConfirmFromOrder(orderEvent.OrderId, line.ProductId, orderEvent.CompletedAt);
            }
        }

        public void Dispatch(OrderCancelledEvent orderEvent)
        {
            ArgumentNullException.ThrowIfNull(orderEvent);
        }
    }
}
