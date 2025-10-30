using Core.Messaging;
using Core.Messaging.Contracts;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Options;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Worker.Consumers.CustomerCreatedConsumer>();

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
    });
});

builder.Services.AddHostedService<Worker.Worker>();

var host = builder.Build();
host.Run();
