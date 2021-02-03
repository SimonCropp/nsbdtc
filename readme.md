# NSB Sql Proxy

When using the NServiceBus Sql transport and also writing to a different Sql database on the same Sql server the .net sql client will incorrectly escalate to DTC. This repo provides a workaround to prevent this behavior by using [Synonyms](https://docs.microsoft.com/en-us/sql/relational-databases/synonyms/synonyms-database-engine).

<!-- toc -->
## Contents

  * [Requirements](#requirements)
  * [Running](#running)
    * [Address patching](#address-patching)<!-- endToc -->



## Requirements

 * Sql instance available on `.`


## Running

 * Run both ShippingInstaller and OrdersInstaller.
 * Run both OrdersEndpoint and ShippingEndpoint
 * Hit `c` on OrdersEndpoint


## Synonyms

Endpoints don't interact with the `NServiceBus` database. Instead they interact with the business database, in this case `Orders` and `Shipping`.

This is achieved by using [Synonyms](https://docs.microsoft.com/en-us/sql/relational-databases/synonyms/synonyms-database-engine).

A utility class from the [NServiceBus.SqlNative project](https://github.com/NServiceBusExtensions/NServiceBus.SqlNative) enables creating Synonyms.

<!-- snippet: https://raw.githubusercontent.com/NServiceBusExtensions/NServiceBus.SqlNative/master/src/SqlServer.Native/Synonym.cs -->
<a id='snippet-https://raw.githubusercontent.com/NServiceBusExtensions/NServiceBus.SqlNative/master/src/SqlServer.Native/Synonym.cs'></a>
```cs
using System.Data.Common;
using System.Threading.Tasks;

namespace NServiceBus.Transport.SqlServerNative
{
    public class Synonym
    {
        DbConnection sourceDatabase;
        string targetDatabase;
        string sourceSchema;
        string targetSchema;
        DbTransaction? sourceTransaction;

        public Synonym(DbConnection sourceDatabase, string targetDatabase, string sourceSchema = "dbo", string targetSchema = "dbo")
        {
            Guard.AgainstNull(sourceDatabase, nameof(sourceDatabase));
            Guard.AgainstNullOrEmpty(targetDatabase, nameof(targetDatabase));
            Guard.AgainstNullOrEmpty(targetSchema, nameof(targetSchema));
            this.sourceDatabase = sourceDatabase;
            this.targetDatabase = targetDatabase;
            this.sourceSchema = sourceSchema;
            this.targetSchema = targetSchema;
        }

        public Synonym(DbTransaction sourceTransaction, string targetDatabase, string sourceSchema = "dbo", string targetSchema = "dbo")
        {
            Guard.AgainstNull(sourceTransaction, nameof(sourceTransaction));
            Guard.AgainstNullOrEmpty(targetDatabase, nameof(targetDatabase));
            Guard.AgainstNullOrEmpty(targetSchema, nameof(targetSchema));
            this.sourceTransaction = sourceTransaction;
            this.targetDatabase = targetDatabase;
            this.sourceSchema = sourceSchema;
            this.targetSchema = targetSchema;
            sourceDatabase = sourceTransaction.Connection;
        }

        public async Task Create(string synonym, string? target = null)
        {
            target ??= synonym;
            GuardAgainstCircularAlias(synonym, target);
            using var command = sourceDatabase.CreateCommand();
            command.Transaction = sourceTransaction;
            command.CommandText = $@"
if not exists (
   select 0
    from sys.synonyms
    inner join sys.schemas on
               synonyms.schema_id = schemas.schema_id
    where synonyms.name = '{target}' and
          schemas.name ='{sourceSchema}'
)
begin
    create synonym [{sourceSchema}].[{synonym}]
    for [{targetDatabase}].[{targetSchema}].[{target}];
end
";
            await command.ExecuteNonQueryAsync();
        }

        public async Task DropAll()
        {
            using var command = sourceDatabase.CreateCommand();
            command.Transaction = sourceTransaction;
            command.CommandText = @"
declare @n char(1)
set @n = char(10)

declare @stmt nvarchar(max)

select @stmt = isnull( @stmt + @n, '' ) +
'drop synonym [' + SCHEMA_NAME(schema_id) + '].[' + name + ']'
from sys.synonyms

exec sp_executesql @stmt
";
            await command.ExecuteNonQueryAsync();
        }

        public async Task Drop(string synonym)
        {
            using var command = sourceDatabase.CreateCommand();
            command.Transaction = sourceTransaction;
            command.CommandText = $@"
if exists (
  select 0
    from sys.synonyms
    inner join sys.schemas on
               synonyms.schema_id = schemas.schema_id
    where synonyms.name = '{synonym}' and
          schemas.name ='{sourceSchema}'
)
begin
    drop synonym [{sourceSchema}].[{synonym}];
end
";
            await command.ExecuteNonQueryAsync();
        }

        void GuardAgainstCircularAlias(string synonym, string target)
        {
            if (targetDatabase == sourceDatabase.Database &&
                synonym == target &&
                sourceSchema == targetSchema)
            {
                throw new("Invalid circular alias.");
            }
        }
    }
}
```
<sup><a href='#snippet-https://raw.githubusercontent.com/NServiceBusExtensions/NServiceBus.SqlNative/master/src/SqlServer.Native/Synonym.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Then an application can use `Synonym.cs` as follows:

<!-- snippet: SynonymInstaller.cs -->
<a id='snippet-SynonymInstaller.cs'></a>
```cs
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Transport.SqlServerNative;

public static class SynonymInstaller
{
    public static async Task Install(
        string endpoint,
        DbConnection nsbConnection,
        DbConnection businessConnection,
        IEnumerable<string>? interactionEndpoints = null)
    {
        Console.WriteLine("Running Synonym installation");
        Synonym synonym = new(businessConnection, "NServiceBus");

        await synonym.Create("error");
        await synonym.Create("audit");
        await synonym.Create("SubscriptionRouting");
        if (interactionEndpoints != null)
        {
            foreach (var interactionEndpoint in interactionEndpoints)
            {
                await synonym.Create(interactionEndpoint);
            }
        }

        foreach (var tableName in await GetEndpointTables(nsbConnection, endpoint))
        {
            await synonym.Create(tableName);
        }
    }

    static async Task<List<string>> GetEndpointTables(DbConnection nsbConnection, string endpoint)
    {
        List<string> names = new();
        await using var command = nsbConnection.CreateCommand();
        command.CommandText = $@"
 select name from sys.objects
    where
        name LIKE '{endpoint}%'
        and type in ('U')
";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = await reader.GetFieldValueAsync<string>(0);
            names.Add(name);
        }

        return names;
    }
}
```
<sup><a href='/Shared/SynonymInstaller.cs#L1-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-SynonymInstaller.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CallSynonymInstaller -->
<a id='snippet-callsynonyminstaller'></a>
```cs
await SynonymInstaller.Install(
    "OrdersEndpoint",
    nsbConnection,
    ordersConnection,
    new List<string> {"ShippingEndpoint"});
```
<sup><a href='/OrdersInstaller/Program.cs#L16-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-callsynonyminstaller' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Address patching

Since the Orders database is being used for the messaging, the SqlTransport using the db name queue names. 

So when sending from Orders the address used is `OrdersEndpoint@[dbo]@[Orders]` when it should be `OrdersEndpoint@[dbo]@[NServiceBus]`.

This manifests in the AustoSubscribe feature and the reply address. Both of these need to be patched.


#### AutoSubscribe

<!-- snippet: AutoSubscribeEx.cs -->
<a id='snippet-AutoSubscribeEx.cs'></a>
```cs
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
```
<sup><a href='/Shared/AutoSubscribeEx.cs#L1-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-AutoSubscribeEx.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### ReplyPatching

<!-- snippet: ReplyPatchingBehavior.cs -->
<a id='snippet-ReplyPatchingBehavior.cs'></a>
```cs
using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class ReplyPatchingBehavior :
    Behavior<IOutgoingPhysicalMessageContext>
{
    string endpointName;

    ReplyPatchingBehavior(string endpointName)
    {
        this.endpointName = endpointName;
    }

    public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
    {
        context.Headers["NServiceBus.ReplyToAddress"] = $"{endpointName}@[dbo]@[NServiceBus]";
        return next();
    }

    public class Step :
        RegisterStep
    {
        public Step(string endpointName)
            : base(
                stepId: "ReplyPatchingBehavior",
                behavior: typeof(ReplyPatchingBehavior),
                description: "Fixes the reply address to be NSB",
                factoryMethod: _ => new ReplyPatchingBehavior(endpointName))
        {
        }

    }
}
```
<sup><a href='/Shared/ReplyPatchingBehavior.cs#L1-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReplyPatchingBehavior.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

