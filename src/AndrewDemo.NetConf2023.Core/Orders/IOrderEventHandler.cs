using AndrewDemo.NetConf2023.Abstract.Orders;

namespace AndrewDemo.NetConf2023.Core.Orders;

/// <summary>
/// 標準 dispatcher 的 scoped 處理端；用途由事件 scope 的 Order context 提供。
/// 有真實商業效果時須透過 ProtectedWriteExecutor.ForOrder；未提供的能力不註冊虛構處理器。
/// </summary>
public interface IOrderEventHandler
{
    void Handle(OrderCompletedEvent orderEvent);
    void Handle(OrderCancelledEvent orderEvent);
}
