using Application;
using Core.Messaging;
using Core.Messaging.Contracts;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Polly;
using Worker;
using Worker.Resilience;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Configurar HttpClient para fazer chamadas à ProposalWebApi com políticas de resiliência
builder.Services.AddHttpClient("ProposalWebApi", client =>
{
    var baseAddress = builder.Configuration["PROPOSAL_WEB_API_BASE_URL"];
    client.BaseAddress = new Uri(baseAddress!);
    client.DefaultRequestHeaders.Add("Accept", "text/plain");
    client.Timeout = TimeSpan.FromSeconds(300);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
})
.AddPolicyHandler(ResiliencePolicies.GetResiliencePolicy());

// Configurar HttpClient para fazer chamadas à CardWebApi com políticas de resiliência
builder.Services.AddHttpClient("CardWebApi", client =>
{
    var baseAddress = builder.Configuration["CARD_WEB_API_BASE_URL"];
    client.BaseAddress = new Uri(baseAddress!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(300);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
})
.AddPolicyHandler(ResiliencePolicies.GetResiliencePolicy());

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
            
            // Configurar retry com exponential backoff
            e.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2));
                
                r.Ignore<ArgumentException>();
            });
            
            // Configurar DLQ após todos os retries falharem
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
            
            // Configurar retry com exponential backoff
            e.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2));
                
                r.Ignore<ArgumentException>(); // Não retry para argumentos inválidos
            });
            
            // Configurar DLQ após todos os retries falharem
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

var host = builder.Build();
host.Run();

namespace Worker
{
    public class Program;
}