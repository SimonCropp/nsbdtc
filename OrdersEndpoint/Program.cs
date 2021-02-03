using NServiceBus;
using NServiceBus.Persistence.Sql;

var configuration = new EndpointConfiguration("OrdersEndpoint");
// note that transport and persistence is using the business DB
configuration.ApplyCommonConfig(Connections.Orders);

configuration.RegisterComponents(c =>
{
    c.ConfigureComponent(b =>
        {
            var session = b.Build<ISqlStorageSession>();

            // Since now using the same connection the session can be used
            var context = DbContextBuilder.Build(session.Connection, session.Transaction);

            //Ensure context is flushed before the ISqlStorageSession transaction is committed
            session.OnSaveChanges(_ => context.SaveChangesAsync());

            return context;
        },
        DependencyLifecycle.InstancePerUnitOfWork);
});

var endpointInstance = await Endpoint.Start(configuration);
await MessageSender.StartLoop(endpointInstance);
await endpointInstance.Stop();