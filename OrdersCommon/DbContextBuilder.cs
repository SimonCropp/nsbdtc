using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public static class DbContextBuilder
{
    public static MyDataContext Build()
    {
        var contextOptions = new DbContextOptionsBuilder<MyDataContext>()
            .UseSqlServer(Connections.Business)
            .Options;
        return new MyDataContext(contextOptions);
    }
    public static MyDataContext Build(DbConnection connection, DbTransaction transaction)
    {
        var contextOptions = new DbContextOptionsBuilder<MyDataContext>()
            .UseSqlServer(connection)
            .Options;
        var context = new MyDataContext(contextOptions);
        context.Database.UseTransaction(transaction);
        return context;
    }

    public static async Task EnsureExists()
    {
        using var dataContext = Build();
        await dataContext.Database.EnsureCreatedAsync();
    }
}