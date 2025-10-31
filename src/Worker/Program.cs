using Core.Messaging;
using Core.Messaging.Contracts;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// Configurar HttpClient para fazer chamadas à ProposalWebApi
builder.Services.AddHttpClient("ProposalWebApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44393");
    client.DefaultRequestHeaders.Add("Accept", "text/plain");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// Configurar HttpClient para fazer chamadas à CardWebApi
builder.Services.AddHttpClient("CardWebApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44309");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

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
        });

        cfg.ReceiveEndpoint(MessagingConstants.ProposalApprovedQueue, e =>
        {
            e.ConfigureConsumer<Worker.Consumers.ProposalApprovedConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<Worker.Worker>();

var host = builder.Build();
host.Run();
