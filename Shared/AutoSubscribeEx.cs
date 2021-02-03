using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Transport.SqlServerNative;
using NServiceBus.Unicast;

/// <summary>
/// With sql synonym proxy, the built in <see cref="AutoSubscribe"/> results
/// in the sql transport using the business database name for the routing
/// </summary>
class AutoSubscribeEx : Feature
{
    public AutoSubscribeEx()
    {
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't autosubscribe.");
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var conventions = context.Settings.Get<Conventions>();
        var endpointName = context.Settings.EndpointName();
        //TODO: work out how to extract the transport connection from NSB
        var nsbConnectionString = context.Settings.Get<string>("NServiceBusConnectionString");
        context.RegisterStartupTask(b =>
        {
            var handlerRegistry = b.Build<MessageHandlerRegistry>();
            var messageTypesHandled = GetMessageTypesHandled(handlerRegistry, conventions);
            return new ApplySubscriptions(messageTypesHandled, nsbConnectionString, endpointName);
        });
    }

    static List<Type> GetMessageTypesHandled(MessageHandlerRegistry handlerRegistry, Conventions conventions)
    {
        //get all potential messages
        return handlerRegistry.GetMessageTypes()

            //never auto-subscribe system messages
            .Where(t => !conventions.IsInSystemConventionList(t))

            //commands should never be subscribed to
            .Where(t => !conventions.IsCommandType(t))

            //only events unless the user asked for all messages
            .Where(t => conventions.IsEventType(t))

            .ToList();
    }

    class ApplySubscriptions : FeatureStartupTask
    {
        List<Type> messageTypesHandled;
        string connectionString;
        static ILog logger = LogManager.GetLogger<AutoSubscribeEx>();
        string address;
        string endpoint;

        public ApplySubscriptions(List<Type> messageTypesHandled, string connectionString, string endpoint)
        {
            this.messageTypesHandled = messageTypesHandled;
            this.connectionString = connectionString;

            address = $"{endpoint}@[dbo]@[NServiceBus]";
            this.endpoint = endpoint;
        }

        protected override async Task OnStart(IMessageSession session)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            var subscriptionManager = new SubscriptionManager(new Table("SubscriptionRouting"), sqlConnection);
            foreach (var type in messageTypesHandled)
            {
                try
                {
                    await subscriptionManager.Subscribe(endpoint, address, type.FullName!);
                }
                catch (Exception e)
                {
                    logger.Error($"AutoSubscribe was unable to subscribe to event '{type.FullName}'", e);
                }
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            return Task.CompletedTask;
        }
    }
}