using Core.Messaging;
using Core.Messaging.Contracts;
using Infrastructure;
using Infrastructure.Messaging;
using IntegrationTests.Helpers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Worker;
using Worker.Resilience;

namespace IntegrationTests.Factories;

public class WorkerHostFactory : IAsyncDisposable
{
    private readonly IHost _host;

    public WorkerHostFactory(
        string postgresConnectionString,
        string rabbitMqConnectionString,
        string proposalWebApiBaseUrl,
        string cardWebApiBaseUrl)
    {
        var builder = Host.CreateApplicationBuilder();
        
        var rabbitUri = new Uri(rabbitMqConnectionString);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgresConnection"] = postgresConnectionString,
            ["RabbitMQ:Host"] = rabbitUri.Host,
            ["RabbitMQ:Port"] = rabbitUri.Port > 0 ? rabbitUri.Port.ToString() : "5672",
            ["RabbitMQ:Username"] = "guest",
            ["RabbitMQ:Password"] = "guest",
            ["RabbitMQ:VirtualHost"] = "/"
        });

        // Adicionar Infrastructure (mesma lógica do Program.cs)
        builder.Services.AddInfrastructure(builder.Configuration);

        // Configurar HttpClients com as URLs das APIs de teste (mesma lógica do Program.cs)
        builder.Services.AddHttpClient("ProposalWebApi", client =>
        {
            client.BaseAddress = new Uri(proposalWebApiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "text/plain");
            client.Timeout = TimeSpan.FromSeconds(300);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        .AddPolicyHandler(ResiliencePolicies.GetResiliencePolicy());

        builder.Services.AddHttpClient("CardWebApi", client =>
        {
            client.BaseAddress = new Uri(cardWebApiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(300);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        .AddPolicyHandler(ResiliencePolicies.GetResiliencePolicy());

        // Configurar MassTransit (mesma lógica do Program.cs)
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<Worker.Consumers.CustomerCreatedConsumer>();
            x.AddConsumer<Worker.Consumers.ProposalApprovedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var vhost = (options.VirtualHost ?? "/").Trim();
                vhost = Uri.EscapeDataString(vhost.TrimStart('/'));
                var hostUri = new Uri($"rabbitmq://{options.Host}:{options.Port}/{vhost}");

                cfg.Host(hostUri, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ReceiveEndpoint(MessagingConstants.CustomerCreatedQueue, e =>
                {
                    e.ConfigureConsumer<Worker.Consumers.CustomerCreatedConsumer>(context);
                    
                    e.UseMessageRetry(r =>
                    {
                        r.Exponential(
                            retryLimit: 5,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(30),
                            intervalDelta: TimeSpan.FromSeconds(2));
                        
                        r.Ignore<ArgumentException>();
                    });
                    
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromMinutes(1),
                            maxInterval: TimeSpan.FromMinutes(10),
                            intervalDelta: TimeSpan.FromMinutes(2));
                    });
                });

                cfg.ReceiveEndpoint(MessagingConstants.ProposalApprovedQueue, e =>
                {
                    e.ConfigureConsumer<Worker.Consumers.ProposalApprovedConsumer>(context);
                    
                    e.UseMessageRetry(r =>
                    {
                        r.Exponential(
                            retryLimit: 5,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(30),
                            intervalDelta: TimeSpan.FromSeconds(2));
                        
                        r.Ignore<ArgumentException>();
                    });
                    
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromMinutes(1),
                            maxInterval: TimeSpan.FromMinutes(10),
                            intervalDelta: TimeSpan.FromMinutes(2));
                    });
                });
            });
        });

        _host = builder.Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _host.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}

