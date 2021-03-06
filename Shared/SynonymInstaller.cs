﻿using System;
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