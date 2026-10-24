using AndrewDemo.NetConf2023.Core.Operations;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Core.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.Core.Orders;

/// <summary>每次派送皆開獨立 scope；singleton 只保存 scope factory，不捕捉 request 狀態。</summary>
public sealed class DefaultOrderEventDispatcher(IServiceScopeFactory scopeFactory) : IOrderEventDispatcher
{
    public const string DispatcherId = "default-order-event-dispatcher";

    public void Dispatch(OrderCompletedEvent orderEvent) => DispatchWithResult(orderEvent);

    public OperationResult DispatchWithResult(OrderCompletedEvent orderEvent)
    {
        ArgumentNullException.ThrowIfNull(orderEvent);
        return Dispatch(orderEvent.OrderId, handler => handler.Handle(orderEvent));
    }

    public void Dispatch(OrderCancelledEvent orderEvent) => DispatchWithResult(orderEvent);

    public OperationResult DispatchWithResult(OrderCancelledEvent orderEvent)
    {
        ArgumentNullException.ThrowIfNull(orderEvent);
        return Dispatch(orderEvent.OrderId, handler => handler.Handle(orderEvent));
    }

    private OperationResult Dispatch(int orderId, Action<IOrderEventHandler> handle)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var operation = services.GetRequiredService<OperationExecution>();
        operation.Begin();
        var purposeLoaded = false;
        try
        {
            services.GetRequiredService<IdentityContext>().Initialize(IdentitySnapshot.System);
            services.GetRequiredService<OrderContextLoader>().Bind(orderId);
            purposeLoaded = true;
            // 先確認保存用途才解析處理器；空集合不虛構略過紀錄。
            foreach (var handler in services.GetServices<IOrderEventHandler>()) handle(handler);
            services.GetRequiredService<OrderContextLoader>().RequireCurrent(orderId);
            return operation.Complete(OperationOutcome.Succeeded)!;
        }
        catch (Exception error)
        {
            if (!purposeLoaded) operation.InvalidatePurpose();
            operation.Fail(error);
            throw;
        }
    }
}
