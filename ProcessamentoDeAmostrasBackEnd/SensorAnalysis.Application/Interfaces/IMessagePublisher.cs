using SensorAnalysis.Domain.Events;

namespace SensorAnalysis.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(SensorAnomalyDetected domainEvent);
}


