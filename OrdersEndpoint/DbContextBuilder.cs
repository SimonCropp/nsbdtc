using System.Data.Common;
using Microsoft.EntityFrameworkCore;

static class DbContextBuilder
{
    public static OrdersDbContext Build(DbConnection connection, DbTransaction transaction)
    {
        var contextOptions = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer(connection)
            .Options;
        var context = new OrdersDbContext(contextOptions);
        context.Database.UseTransaction(transaction);
        return context;
    }
}