using AndrewDemo.NetConf2023.Abstract.Orders;

namespace AndrewDemo.NetConf2023.Core.Tests;

// 僅供不驗證事件邊界的 Checkout／controller 測試；正式派送另有獨立 scope 整合測試。
internal sealed class StubOrderEventDispatcher : IOrderEventDispatcher
{
    public void Dispatch(OrderCompletedEvent orderEvent) { }
    public void Dispatch(OrderCancelledEvent orderEvent) { }
}
