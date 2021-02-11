# NSB Sql Proxy

When using the NServiceBus Sql transport and also writing to a different Sql database on the same Sql server the .net sql client will incorrectly escalate to DTC. This repo provides a workaround to prevent this behavior by using [Synonyms](https://docs.microsoft.com/en-us/sql/relational-databases/synonyms/synonyms-database-engine).

<!-- toc -->
## Contents

  * [Requirements](#requirements)
  * [Running](#running)
  * [Synonyms](#synonyms)<!-- endToc -->



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
