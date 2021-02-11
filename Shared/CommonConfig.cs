using System;
using Microsoft.Data.SqlClient;
using NServiceBus;

public static class CommonConfig
{
    public static void ApplyCommonConfig(this EndpointConfiguration configuration, string connectionString)
    {
        var transport = configuration.UseTransport<SqlServerTransport>();

        transport.UseCustomSqlConnectionFactory(async () =>
        {
            SqlConnection connection = null;

            try
            {
                connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.ChangeDatabaseAsync("NServiceBus");

                return connection;
            }
            catch
            {
                if(connection != null)
                {
                    await connection.DisposeAsync();
                }

                throw;
            }
        });

        transport.UseCatalogForQueue("ShippingEndpoint", "NServiceBus");
        transport.UseCatalogForQueue("ShippingEndpoint.Delayed", "NServiceBus");
        transport.UseCatalogForQueue("OrdersEndpoint", "NServiceBus");
        transport.UseCatalogForQueue("OrdersEndpoint.Delayed", "NServiceBus");
        transport.UseCatalogForQueue("error", "NServiceBus");
        transport.UseCatalogForQueue("audit", "NServiceBus");

        transport.SubscriptionSettings().SubscriptionTableName("SubscriptionRouting", "dbo", "NServiceBus");
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.NativeDelayedDelivery();

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(_ => _.NumberOfRetries(0));
        recoverability.Immediate(_ => _.NumberOfRetries(0));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();

        persistence.ConnectionBuilder(() => new SqlConnection(connectionString));

        configuration.SendFailedMessagesTo("error");
        configuration.AuditProcessedMessagesTo("audit");
    }
}