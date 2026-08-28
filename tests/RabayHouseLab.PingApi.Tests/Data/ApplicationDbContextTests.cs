using Microsoft.EntityFrameworkCore;
using RabayHouseLab.PingApi.Api.Data;

namespace RabayHouseLab.PingApi.Tests.Data;

public sealed class ApplicationDbContextTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    [Fact]
    public void CanInstantiateWithInMemoryOptions()
    {
        using var context = new ApplicationDbContext(CreateOptions(Guid.NewGuid().ToString()));

        Assert.NotNull(context);
        Assert.NotNull(context.Database);
    }

    [Fact]
    public async Task CanCreateDatabaseInMemoryAsync()
    {
        using var context = new ApplicationDbContext(CreateOptions(Guid.NewGuid().ToString()));

        var created = await context.Database.EnsureCreatedAsync();

        Assert.True(created);
        Assert.True(await context.Database.CanConnectAsync());
    }

    [Fact]
    public async Task CanDisposeAndRecreateWithSameStoreAsync()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var ctx1 = new ApplicationDbContext(CreateOptions(dbName)))
        {
            Assert.True(await ctx1.Database.EnsureCreatedAsync());
        }

        using (var ctx2 = new ApplicationDbContext(CreateOptions(dbName)))
        {
            Assert.True(await ctx2.Database.CanConnectAsync());
        }
    }
}
