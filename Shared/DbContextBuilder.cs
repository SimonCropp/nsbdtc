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

    public static async Task EnsureExists()
    {
        using var dataContext = Build();
        await dataContext.Database.EnsureCreatedAsync();
    }
}