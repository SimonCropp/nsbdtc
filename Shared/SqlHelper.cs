using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public static class SqlHelper
{
    public static async Task EnsureDatabaseExists(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString);
        var database = builder.InitialCatalog;

        var masterConnectionString = connectionString.Replace(builder.InitialCatalog, "master");

        await using SqlConnection masterConnection = new(masterConnectionString);
        await masterConnection.OpenAsync();

        await using var command = masterConnection.CreateCommand();
        command.CommandText = $@"
if(db_id('{database}') is null)
    create database [{database}]
";
        await command.ExecuteNonQueryAsync();
    }
}