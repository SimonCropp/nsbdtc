using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Logging;

public class MyCommandHandler : IHandleMessages<MyCommand>
{
    static ILog log = LogManager.GetLogger<MyCommandHandler>();

    public Task Handle(MyCommand commandMessage, IMessageHandlerContext context)
    {
        using (var connection = new SqlConnection("Data Source=VM-DEV-LBS12;Database=OtherDb;Integrated Security=True;Max Pool Size=100"))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.CommandText = "INSERT INTO Table_1 (id) VALUES (@val1)";
                command.Parameters.AddWithValue("@val1", Guid.NewGuid());
                command.ExecuteNonQuery();
            }
        }

        log.Info($"Hello from {nameof(MyCommandHandler)}");
        return Task.CompletedTask;
    }
}