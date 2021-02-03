using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

await CreateOrdersDb();

await using var ordersConnection = await Connections.OpenOrders();

await SqlHelper.EnsureDatabaseExists(Connections.NServiceBus);

await using var nsbConnection = await Connections.OpenNServiceBus();

await QueueInstaller.Install("OrdersEndpoint", nsbConnection);
await PersistenceInstaller.Install("OrdersEndpoint", Connections.NServiceBus);
await SynonymInstaller.Install("OrdersEndpoint", nsbConnection, ordersConnection);


async Task CreateOrdersDb()
{
    var options = new DbContextOptionsBuilder<OrdersDbContext>()
        .UseSqlServer(Connections.Orders)
        .Options;
    using var context = new OrdersDbContext(options);
    await context.Database.EnsureCreatedAsync();
}