using Infrastructure;
using Application;
using MassTransit;
using Microsoft.Extensions.Options;
using Infrastructure.Messaging;
using FluentValidation;
using FluentValidation.AspNetCore;
using CardWebApi.Controllers.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCardRequestDtoValidator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
