namespace SensorAnalysis.Domain.Events;

public sealed class SensorAnomalyDetected
{
    public string SensorId { get; }
    public DateTime Timestamp { get; }
    public string Reason { get; }

    private SensorAnomalyDetected(string sensorId, DateTime timestamp, string reason)
    {
        SensorId = sensorId;
        Timestamp = timestamp;
        Reason = reason;
    }

    public static SensorAnomalyDetected Create(string sensorId, DateTime timestamp, string reason)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new ArgumentException("SensorId cannot be empty", nameof(sensorId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        return new SensorAnomalyDetected(sensorId, timestamp, reason);
    }
}
