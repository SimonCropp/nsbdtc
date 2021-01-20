using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var configuration = new EndpointConfiguration("Endpoint");
       // configuration.EnableInstallers();
        var transport = configuration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(MyCommandHandler.Connection);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.NativeDelayedDelivery();

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() => new SqlConnection(MyCommandHandler.Connection));

        configuration.PurgeOnStartup(true);
        var endpointInstance = await Endpoint.Start(configuration)
            .ConfigureAwait(false);
        await MessageSender.SendMessages(endpointInstance);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}