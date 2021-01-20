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
        await SendMessages(endpointInstance);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }

    static async Task SendMessages(IMessageSession messageSession)
    {
        Console.WriteLine("Press [c] to send a command. Press [Esc] to exit.");
        while (true)
        {
            var input = Console.ReadKey();
            Console.WriteLine();

            switch (input.Key)
            {
                case ConsoleKey.C:
                    await messageSession.SendLocal(new MyCommand());
                    break;
                case ConsoleKey.Escape:
                    return;
            }
        }
    }
}