using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Exceptions;

namespace SensorAnalysis.Domain.Events;

public sealed class SensorAnomalyDetected : IDomainEvent
{
    public string SensorId { get; }
    public DateTime Timestamp { get; }
    public string Reason { get; }
    public DateTime OccurredAt { get; }

    private SensorAnomalyDetected(string sensorId, DateTime timestamp, string reason)
    {
        SensorId = sensorId;
        Timestamp = timestamp;
        Reason = reason;
        OccurredAt = DateTime.UtcNow;
    }

    public static SensorAnomalyDetected Create(string sensorId, DateTime timestamp, string reason)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new DomainException("SensorId cannot be empty");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason cannot be empty");

        return new SensorAnomalyDetected(sensorId, timestamp, reason);
    }
}
