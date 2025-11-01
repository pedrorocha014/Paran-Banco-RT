using CardWebApi;
using IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests.Factories;

public class CardWebApiFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;
    private readonly string _rabbitMqConnectionString;

    public CardWebApiFactory(string postgresConnectionString, string rabbitMqConnectionString)
    {
        _postgresConnectionString = postgresConnectionString;
        _rabbitMqConnectionString = rabbitMqConnectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureHostConfiguration(config =>
        {
            var rabbitUri = new Uri(_rabbitMqConnectionString);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresConnection"] = _postgresConnectionString,
                ["RabbitMQ:Host"] = rabbitUri.Host,
                ["RabbitMQ:Port"] = rabbitUri.Port > 0 ? rabbitUri.Port.ToString() : "5672",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest",
                ["RabbitMQ:VirtualHost"] = "/"
            });
        });

        return base.CreateHost(builder);
    }
}

