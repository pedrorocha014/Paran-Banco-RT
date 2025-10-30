using Core.CustomerAggregate;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IGenericRepository<Customer>, GenericRepository<Customer>>();

        var rabbitSection = configuration.GetSection(RabbitMqOptions.SectionName);
        var rabbitOptions = new RabbitMqOptions
        {
            Host = rabbitSection["Host"] ?? "localhost",
            Username = rabbitSection["Username"] ?? "guest",
            Password = rabbitSection["Password"] ?? "guest",
            VirtualHost = rabbitSection["VirtualHost"] ?? "/",
        };
        if (int.TryParse(rabbitSection["Port"], out var port))
        {
            rabbitOptions.Port = port;
        }
        services.AddSingleton<IOptions<RabbitMqOptions>>(Options.Create(rabbitOptions));

        return services;
    }
}


