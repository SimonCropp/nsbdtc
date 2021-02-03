using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;

var configuration = new EndpointConfiguration("OrdersEndpoint");
configuration.SendFailedMessagesTo("error");
configuration.AuditProcessedMessagesTo("audit");
configuration.PurgeOnStartup(true);
var transport = configuration.UseTransport<SqlServerTransport>();

// note that transport is connecting to the Orders DB
transport.ConnectionString(Connections.Orders);
transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
transport.NativeDelayedDelivery();

var persistence = configuration.UsePersistence<SqlPersistence>();
persistence.SqlDialect<SqlDialect.MsSqlServer>();

// note that persistence is connecting to the Orders DB
persistence.ConnectionBuilder(() => new SqlConnection(Connections.Orders));

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