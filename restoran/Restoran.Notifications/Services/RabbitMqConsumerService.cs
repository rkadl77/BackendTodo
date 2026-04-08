using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Restoran.Notifications.Data;
using Restoran.Notifications.DTOs;
using Restoran.Notifications.Hubs;
using Restoran.Notifications.Models;

namespace Restoran.Notifications.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "order_status_queue";

    public RabbitMqConsumerService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);
        }
        catch
        {
            // RabbitMQ not available - run without messaging
        }

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<OrderStatusMessage>(body);

            if (message == null)
            {
                _channel?.BasicNack(args.DeliveryTag, false, false);
                return;
            }

            var text = $"Статус вашего заказа {message.OrderId} изменился на: {message.Status}";

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            var notification = new Notification
            {
                UserId = message.UserId,
                OrderId = message.OrderId,
                Message = text
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            var dto = new NotificationDto
            {
                Id = notification.Id,
                OrderId = notification.OrderId,
                Message = notification.Message,
                Status = notification.Status,
                CreatedAt = notification.CreatedAt
            };

            await _hubContext.Clients
                .Group($"user_{message.UserId}")
                .SendAsync("ReceiveNotification", dto);

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch
        {
            _channel?.BasicNack(args.DeliveryTag, false, true);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class OrderStatusMessage
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
