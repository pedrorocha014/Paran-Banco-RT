using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests.Helpers;

public static class TestContainersHelper
{
    private static PostgreSqlContainer? _postgresContainer;
    private static RabbitMqContainer? _rabbitMqContainer;

    public static PostgreSqlContainer PostgresContainer
    {
        get
        {
            if (_postgresContainer == null)
            {
                _postgresContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:16")
                    .WithDatabase("parana_banco_rt_db")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();
            }
            return _postgresContainer;
        }
    }

    public static RabbitMqContainer RabbitMqContainer
    {
        get
        {
            if (_rabbitMqContainer == null)
            {
                _rabbitMqContainer = new RabbitMqBuilder()
                    .WithImage("rabbitmq:3.13-management")
                    .WithUsername("guest")
                    .WithPassword("guest")
                    .Build();
            }
            return _rabbitMqContainer;
        }
    }

    public static async Task InitializeAsync()
    {
        await PostgresContainer.StartAsync();
        await RabbitMqContainer.StartAsync();
        
        PostgresHelper.SetConnectionString(PostgresContainer.GetConnectionString());
    }

    public static async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
            _postgresContainer = null;
        }

        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.DisposeAsync();
            _rabbitMqContainer = null;
        }
    }

    public static string GetRabbitMqConnectionString()
    {
        if (_rabbitMqContainer == null)
            throw new InvalidOperationException("RabbitMQ container not initialized");

        var host = _rabbitMqContainer.Hostname;
        var port = _rabbitMqContainer.GetMappedPublicPort(5672);
        
        return $"amqp://{host}:{port}/";
    }

    public static (string Host, int Port) GetRabbitMqConnectionInfo()
    {
        if (_rabbitMqContainer == null)
            throw new InvalidOperationException("RabbitMQ container not initialized");

        return (_rabbitMqContainer.Hostname, _rabbitMqContainer.GetMappedPublicPort(5672));
    }
}

