using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class CreateOrderHandler : IHandleMessages<CreateOrder>
{
    MyDataContext dataContext;
    static ILog log = LogManager.GetLogger<CreateOrderHandler>();

    public CreateOrderHandler(MyDataContext dataContext)
    {
        this.dataContext = dataContext;
    }
    public Task Handle(CreateOrder commandMessage, IMessageHandlerContext context)
    {
        dataContext.Orders.Add(
            new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                Value = 10
            });
        log.Info($"Hello from {nameof(CreateOrderHandler)}");
        return Task.CompletedTask;
    }
}