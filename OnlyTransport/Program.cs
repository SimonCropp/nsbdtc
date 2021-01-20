using System;
using System.Threading.Tasks;
using NServiceBus;

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

        configuration.PurgeOnStartup(true);
        var endpointInstance = await Endpoint.Start(configuration)
            .ConfigureAwait(false);
        await MessageSender.SendMessages(endpointInstance);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}