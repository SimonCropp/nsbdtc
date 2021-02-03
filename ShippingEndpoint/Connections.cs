using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public static class Connections
{
    public static string NServiceBus = "Data Source=.;Database=NServiceBus;Integrated Security=True;Max Pool Size=100";
    public static string Shipping = "Data Source=.;Database=Shipping;Integrated Security=True;Max Pool Size=100";

    public static async Task<SqlConnection> OpenNServiceBus()
    {
        var connection = new SqlConnection(NServiceBus);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<SqlConnection> OpenShipping()
    {
        var connection = new SqlConnection(Shipping);
        await connection.OpenAsync();
        return connection;
    }
}