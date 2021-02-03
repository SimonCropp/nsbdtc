using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class CommonConfig
{
    public static void ApplyCommonConfig(this EndpointConfiguration configuration, string nsbConnection)
    {
        var transport = configuration.UseTransport<SqlServerTransport>();

        transport.ConnectionString(nsbConnection);
        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
        transport.NativeDelayedDelivery();

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(_ => _.NumberOfRetries(0));
        recoverability.Immediate(_ => _.NumberOfRetries(0));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();

        persistence.ConnectionBuilder(() => new SqlConnection(nsbConnection));

        configuration.SendFailedMessagesTo("error");
        configuration.AuditProcessedMessagesTo("audit");

        configuration.GetSettings().Set("NServiceBusConnectionString", nsbConnection);
    }
}