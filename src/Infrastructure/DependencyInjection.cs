using Application.Events.Proposal;
using Core.CustomerAggregate;
using Core.CustomerAggregate.Events;
using Core.Interfaces;
using Core.Shared.Events;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Infrastructure.Repository;
using Infrastructure.UnitOfWork;
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
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<ProposalStatus>("proposal_status");
            }));

        services.AddScoped<IGenericRepository<Customer>, GenericRepository<Customer>>();
        services.AddScoped<IGenericRepository<Proposal>, GenericRepository<Proposal>>();
        services.AddScoped<IGenericRepository<Card>, GenericRepository<Card>>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>(); // Melhorar esse nome depois

        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        services.AddScoped<IEventHandler<ProposalCreatedEvent>, ProposalCreatedEventHandler>();
        services.AddSingleton(typeof(IBackgroundTaskQueue<>), typeof(BackgroundTaskQueue<>));
        

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


