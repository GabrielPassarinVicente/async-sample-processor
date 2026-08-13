using SensorAnalysis.Domain.Events;

namespace SensorAnalysis.Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(SensorAnomalyDetected domainEvent);
}
