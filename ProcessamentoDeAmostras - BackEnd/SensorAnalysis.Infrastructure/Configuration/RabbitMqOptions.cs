namespace SensorAnalysis.Infrastructure.Configuration;

public class RabbitMqOptions
{
    public string HostName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string QueueName { get; set; } = "log_notifications";
}