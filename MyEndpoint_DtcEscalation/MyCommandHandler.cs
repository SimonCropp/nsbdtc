using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Logging;

public class MyCommandHandler : IHandleMessages<MyCommand>
{
    static ILog log = LogManager.GetLogger<MyCommandHandler>();

    public async Task Handle(MyCommand commandMessage, IMessageHandlerContext context)
    {
        await using var connection = new SqlConnection(
            "Data Source=.;Database=MyBusiness;Integrated Security=True;Max Pool Size=100");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.Connection = connection;
        command.CommandText = "INSERT INTO Table_1 (id) VALUES (@val1)";
        command.Parameters.AddWithValue("@val1", Guid.NewGuid());
        await command.ExecuteNonQueryAsync();
        log.Info($"Hello from {nameof(MyCommandHandler)}");
    }
}