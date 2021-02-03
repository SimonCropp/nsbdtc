using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using Utilities.NServiceBus;

async Task DoInstall()
{
    await DbContextBuilder.EnsureExists();
    await using var businessConnection = await Connections.OpenBusiness();
    await using var openNServiceBus = await Connections.OpenNServiceBus();
    await SynonymInstaller.Install("MyEndpoint", openNServiceBus, businessConnection);
}

await DoInstall();


var configuration = new EndpointConfiguration("MyEndpoint");
configuration.SendFailedMessagesTo("error");
configuration.AuditProcessedMessagesTo("audit");
configuration.EnableInstallers();
configuration.PurgeOnStartup(true);
var transport = configuration.UseTransport<SqlServerTransport>();

// note that transport is connecting to the Business DB
transport.ConnectionString(Connections.Business);
transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
transport.NativeDelayedDelivery();

var persistence = configuration.UsePersistence<SqlPersistence>();
persistence.SqlDialect<SqlDialect.MsSqlServer>();

// note that persistence is connecting to the Business DB
persistence.ConnectionBuilder(() => new SqlConnection(Connections.Business));

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

