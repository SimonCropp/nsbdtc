using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;

class Program
{
    static async Task Main()
    {
        await DbContextBuilder.EnsureExists();

        var configuration = new EndpointConfiguration("MyEndpoint_DtcEscalation");
        configuration.SendFailedMessagesTo("error");
        configuration.AuditProcessedMessagesTo("audit");
        configuration.EnableInstallers();
        var transport = configuration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(Connections.NServiceBus);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.NativeDelayedDelivery();

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() => new SqlConnection(Connections.NServiceBus));
        configuration.PurgeOnStartup(true);

        configuration.RegisterComponents(c =>
        {
            c.ConfigureComponent(b =>
                {
                    var session = b.Build<ISqlStorageSession>();

                    var context = DbContextBuilder.Build();
                    // Cant use ISqlStorageSession.Transaction since diff databases
                    // instead since transport is TransportTransactionMode.TransactionScope
                    // ef will try to use the ambient TransactionScope
                    // this should work, since two db on same sql instance should not escalate
                    // however it does escalate and an exception is thrown

                    //Ensure context is flushed before the ISqlStorageSession transaction is committed
                    session.OnSaveChanges(_ => context.SaveChangesAsync());

                    return context;
                },
                DependencyLifecycle.InstancePerUnitOfWork);
        });

        var endpointInstance = await Endpoint.Start(configuration);
        await MessageSender.StartLoop(endpointInstance);
        await endpointInstance.Stop();
    }
}