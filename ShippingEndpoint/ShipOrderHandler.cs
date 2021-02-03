using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class ShipOrderHandler :
    IHandleMessages<ShipOrder>
{
    static ILog log = LogManager.GetLogger<ShipOrderHandler>();

    public Task Handle(ShipOrder message, IMessageHandlerContext context)
    {
        log.Info($"ShipOrder {message.OrderId}.");
        return context.Publish(
            new OrderShipped
            {
                OrderId = message.OrderId
            });
    }
}