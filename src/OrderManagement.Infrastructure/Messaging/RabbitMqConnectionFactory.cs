using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _options;

    public RabbitMqConnectionFactory(IConfiguration configuration)
    {
        _options = RabbitMqOptions.From(configuration);
    }

    public RabbitMqOptions Options => _options;

    public IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
        };

        return factory.CreateConnection();
    }
}

