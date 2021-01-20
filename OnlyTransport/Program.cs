using System.Threading.Tasks;
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

        configuration.PurgeOnStartup(true);
        var endpointInstance = await Endpoint.Start(configuration)
            .ConfigureAwait(false);
        await MessageSender.SendMessages(endpointInstance);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}