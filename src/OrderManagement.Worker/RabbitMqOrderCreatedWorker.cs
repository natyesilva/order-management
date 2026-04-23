using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Messages;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderManagement.Worker;

public sealed class RabbitMqOrderCreatedWorker(
    RabbitMqConnectionFactory connectionFactory,
    OrderCreatedProcessor processor,
    ILogger<RabbitMqOrderCreatedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = connectionFactory.Options;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(
                    queue: options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, args) =>
                {
                    var messageId = args.BasicProperties?.MessageId ?? args.DeliveryTag.ToString();
                    var correlationId = args.BasicProperties?.CorrelationId;

                    using var _scope = logger.BeginScope(new Dictionary<string, object>
                    {
                        ["messageId"] = messageId,
                        ["correlationId"] = correlationId ?? ""
                    });

                    var type = args.BasicProperties?.Type;
                    if (!string.Equals(type, "OrderCreated", StringComparison.Ordinal))
                    {
                        logger.LogWarning("Ignorando mensagem com Type={Type}", type);
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                        return;
                    }

                    OrderCreatedEvent? payload;
                    try
                    {
                        payload = JsonSerializer.Deserialize<OrderCreatedEvent>(args.Body.Span);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Payload inválido. Mensagem será descartada (ack).");
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                        return;
                    }

                    if (payload is null)
                    {
                        logger.LogError("Payload nulo. Mensagem será descartada (ack).");
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                        return;
                    }

                    try
                    {
                        await processor.ProcessAsync(payload, messageId, correlationId, stoppingToken);
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    catch (DbUpdateException ex)
                    {
                        // Unique MessageId race -> treat as idempotent success.
                        logger.LogWarning(ex, "DbUpdateException ao processar mensagem; ack para manter idempotência.");
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // shutting down
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Erro ao processar mensagem; requeue.");
                        channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                var consumerTag = channel.BasicConsume(
                    queue: options.QueueName,
                    autoAck: false,
                    consumer: consumer);

                logger.LogInformation("Worker RabbitMQ iniciado. Queue={Queue} ConsumerTag={ConsumerTag}", options.QueueName, consumerTag);

                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                finally
                {
                    try
                    {
                        channel.BasicCancel(consumerTag);
                    }
                    catch
                    {
                        // ignore on shutdown
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao conectar/consumir do RabbitMQ. Tentando novamente em 2s.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
