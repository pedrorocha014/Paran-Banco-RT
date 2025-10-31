using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application;
using Application.Middleware;
using MassTransit;
using Microsoft.Extensions.Options;
using Infrastructure.Messaging;
using ProposalWebApi.BackgroundServices;
using Core.CustomerAggregate.Events;
using Core.Shared.Events;
using Application.Events.Proposal;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProposalWebApi.Controllers.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProposalRequestDtoValidator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHostedService<QueuedHostedService<ProposalCreatedEvent>>();

builder.Services.AddMassTransit(x =>
{
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
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
