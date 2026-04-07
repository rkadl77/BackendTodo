using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Restoran.Backend.Models;

namespace Restoran.Backend.Messaging;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "order_status_exchange";
    private const string QueueName = "order_status_queue";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, string.Empty);
        }
        catch
        {
            _connection = null!;
            _channel = null!;
        }
    }

    public Task PublishOrderStatusChangedAsync(Guid orderId, Guid userId, OrderStatus status)
    {
        if (_channel == null) return Task.CompletedTask;

        var message = new OrderStatusMessage
        {
            OrderId = orderId,
            UserId = userId,
            Status = status.ToString(),
            ChangedAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(ExchangeName, string.Empty, props, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

public class OrderStatusMessage
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
