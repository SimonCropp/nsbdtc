using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public static class Connections
{
    public static string NServiceBus = "Data Source=.;Database=NServiceBus;Integrated Security=True;Max Pool Size=100";
    public static string Orders = "Data Source=.;Database=Orders;Integrated Security=True;Max Pool Size=100";

    public static async Task<SqlConnection> OpenNServiceBus()
    {
        var connection = new SqlConnection(NServiceBus);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<SqlConnection> OpenOrders()
    {
        var connection = new SqlConnection(Orders);
        await connection.OpenAsync();
        return connection;
    }
}