using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;

namespace MovieRaterApi.Tests.Unit;

public static class TestHelpers
{
    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
