using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;

public class OrdersSaga :
    SqlSaga<OrdersSaga.OrdersSagaData>,
    IAmStartedByMessages<CreateOrder>,
    IHandleMessages<OrderShipped>
{
    OrdersDbContext dbContext;
    static ILog log = LogManager.GetLogger<OrdersSaga>();

    public OrdersSaga(OrdersDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    protected override string CorrelationPropertyName => nameof(OrdersSagaData.OrderId);

    protected override void ConfigureMapping(IMessagePropertyMapper mapper)
    {
        mapper.ConfigureMapping<CreateOrder>(m => m.OrderId);
        mapper.ConfigureMapping<OrderShipped>(m => m.OrderId);
    }

    public Task Handle(CreateOrder message, IMessageHandlerContext context)
    {
        var orderId = message.OrderId;
        dbContext.Orders.Add(
            new Order
            {
                OrderId = orderId,
                Value = "An order"
            });
        log.Info($"{nameof(OrdersSaga)}: Recevied CreateOrder. Sending ShipOrder");
        return context.Send(
            "ShippingEndpoint",
            new ShipOrder
            {
                OrderId = orderId
            });
    }

    public class OrdersSagaData :
        ContainSagaData
    {
        public virtual Guid OrderId { get; set; }
    }

    public Task Handle(OrderShipped message, IMessageHandlerContext context)
    {
        log.Info($"{nameof(OrdersSaga)}: Recevied OrderShipped. MarkAsComplete");
        MarkAsComplete();
        return Task.CompletedTask;
    }
}