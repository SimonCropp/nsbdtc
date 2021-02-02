using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var configuration = new EndpointConfiguration("MyEndpoint_DtcEscalation");
        configuration.SendFailedMessagesTo("error");
        configuration.AuditProcessedMessagesTo("audit");
        configuration.EnableInstallers();
        var transport = configuration.UseTransport<SqlServerTransport>();
        var connection = "Data Source=.;Database=NServiceBus;Integrated Security=True;Max Pool Size=100";
        transport.ConnectionString(connection);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.NativeDelayedDelivery();

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() => new SqlConnection(connection));

        configuration.PurgeOnStartup(true);
        var endpointInstance = await Endpoint.Start(configuration);
        await MessageSender.StartLoop(endpointInstance);
        await endpointInstance.Stop();
    }
}