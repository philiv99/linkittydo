using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LinkittyDo.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LinkittyDoDbContext>
{
    public LinkittyDoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("MySql connection string not configured");

        var optionsBuilder = new DbContextOptionsBuilder<LinkittyDoDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new LinkittyDoDbContext(optionsBuilder.Options);
    }
}
