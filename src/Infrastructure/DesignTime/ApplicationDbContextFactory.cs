using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Core.CustomerAggregate;

namespace Infrastructure.DesignTime;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")
                               ?? "Host=localhost;Port=5432;Database=parana_banco_rt_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MapEnum<ProposalStatus>("proposal_status");
        });
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}


