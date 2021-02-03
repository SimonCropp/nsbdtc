using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Transport.SqlServerNative;

public static class QueueInstaller
{
    public static async Task Install(string endpointName, DbConnection connection)
    {
        Console.WriteLine("Running Queue installation");
        await CreateQueue("audit", connection);
        await CreateQueue("error", connection);
        await CreateQueue(endpointName, connection);
        DelayedQueueManager delayed = new($"{endpointName}.Delayed", connection);
        await delayed.Create();
        SubscriptionManager subscription = new("SubscriptionRouting", connection);
        await subscription.Create();

        Console.WriteLine("Transaction committed - queue installer complete");
    }

    static Task CreateQueue(string queueName, DbConnection connection)
    {
        QueueManager manager = new(queueName, connection);
        return manager.Create();
    }
}