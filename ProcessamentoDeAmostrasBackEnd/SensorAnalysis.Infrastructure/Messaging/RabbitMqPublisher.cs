using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Infrastructure.Configuration;

namespace SensorAnalysis.Infrastructure.Messaging;

internal class RabbitMqNotificationDto
{
    [JsonPropertyName("sensor_id")]
    public string SensorId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("motivo")]
    public string Reason { get; set; } = string.Empty;
}

internal sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(SensorAnomalyDetected domainEvent)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = "/"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var dto = new RabbitMqNotificationDto
            {
                SensorId = domainEvent.SensorId,
                Timestamp = domainEvent.Timestamp.ToString("O"),
                Reason = domainEvent.Reason
            };

            string json = JsonSerializer.Serialize(dto);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                body: body);

            _logger.LogInformation("✅ [RabbitMQ] Mensagem publicada com sucesso: {Message}", json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RabbitMQ ERRO] Falha ao publicar na fila {Queue}", _options.QueueName);
        }
    }
}