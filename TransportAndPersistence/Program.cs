using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts = true,
    MySqlScripts = false,
    OracleScripts = false,
    ProduceTimeoutScripts = false,
    ProduceOutboxScripts = false,
    ProduceSubscriptionScripts = false
)]
class Program
{
    static async Task Main()
    {
        var configuration = new EndpointConfiguration("Endpoint");
       // configuration.EnableInstallers();
        var transport = configuration.UseTransport<SqlServerTransport>();
        var connection = "Data Source=VM-DEV-LBS12;Database=OtherDb;Integrated Security=True;Max Pool Size=100";
        transport.ConnectionString(connection);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.NativeDelayedDelivery();

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() => new SqlConnection(connection));

        configuration.PurgeOnStartup(true);
        var endpointInstance = await Endpoint.Start(configuration)
            .ConfigureAwait(false);
        await MessageSender.SendMessages(endpointInstance);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}