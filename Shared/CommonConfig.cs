using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

public static class CommonConfig
{
    public static void ApplyCommonConfig(this EndpointConfiguration configuration, string nsbConnection)
    {
        var transport = configuration.UseTransport<SqlServerTransport>();

        transport.ConnectionString(nsbConnection);
        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
        transport.NativeDelayedDelivery();

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();

        persistence.ConnectionBuilder(() => new SqlConnection(nsbConnection));

        var endpointName = configuration.GetSettings().EndpointName();
        configuration.Pipeline.Register(new ReplyPatchingBehavior.Step(endpointName));

        configuration.SendFailedMessagesTo("error");
        configuration.AuditProcessedMessagesTo("audit");

        configuration.GetSettings().Set("NServiceBusConnectionString", nsbConnection);
        // disable AutoSubscribe and replace with AutoSubscribeEx
        configuration.DisableFeature<AutoSubscribe>();
        configuration.EnableFeature<AutoSubscribeEx>();
    }
}