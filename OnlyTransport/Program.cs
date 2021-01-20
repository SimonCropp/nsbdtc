using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var configuration = new EndpointConfiguration("Endpoint");
        configuration.EnableInstallers();

        var transport = configuration.UseTransport<SqlServerTransport>();
        var connection = "Data Source=.;Database=SqlServerSimple;Integrated Security=True;Max Pool Size=100";
        transport.ConnectionString(connection);

        transport.Transactions(TransportTransactionMode.TransactionScope);

        configuration.PurgeOnStartup(true);
        SqlHelper.EnsureDatabaseExists(connection);
        var endpointInstance = await Endpoint.Start(configuration)
            .ConfigureAwait(false);
        await endpointInstance.SendLocal(new MyCommand());
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}