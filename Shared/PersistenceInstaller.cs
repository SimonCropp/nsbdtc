using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Persistence.Sql;

public static class PersistenceInstaller
{
    public static Task Install(string endpointName, string connectionString)
    {
        Console.WriteLine("Running Persistence installation");
        var scriptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"NServiceBus.Persistence.Sql\MsSqlServer");
        var tablePrefix = endpointName + "_";
        return ScriptRunner.Install(
            sqlDialect: new SqlDialect.MsSqlServer(),
            tablePrefix: tablePrefix,
            connectionBuilder: () => new SqlConnection(connectionString),
            scriptDirectory: scriptDirectory,
            shouldInstallOutbox: false,
            shouldInstallTimeouts: false);
    }
}