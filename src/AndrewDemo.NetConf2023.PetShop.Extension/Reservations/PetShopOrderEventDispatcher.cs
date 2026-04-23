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
                var result = _reservationService.ConfirmFromOrder(orderEvent.OrderId, line.ProductId, orderEvent.CompletedAt);
                if (!result.IsConfirmedNow)
                {
                    continue;
                }

                Console.WriteLine(
                    $"[PetShop] reservation confirmed; notify staff and customer. orderId={orderEvent.OrderId}; reservationId={result.ReservationId}; productId={result.ProductId}; staffId={result.StaffId}; buyerMemberId={result.BuyerMemberId}");
            }
        }

        public void Dispatch(OrderCancelledEvent orderEvent)
        {
            ArgumentNullException.ThrowIfNull(orderEvent);
        }
    }
}
