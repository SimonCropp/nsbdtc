using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;

public class OrdersSaga :
    SqlSaga<OrdersSaga.OrdersSagaData>,
    IAmStartedByMessages<CreateOrder>
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
    }
    public Task Handle(CreateOrder message, IMessageHandlerContext context)
    {
        dbContext.Orders.Add(
            new Order
            {
                OrderId = Guid.NewGuid(),
                Value = 10
            });
        log.Info($"Hello from {nameof(OrdersSaga)}");
        return Task.CompletedTask;
    }

    public class OrdersSagaData :
        ContainSagaData
    {
        public virtual Guid OrderId { get; set; }
    }
}