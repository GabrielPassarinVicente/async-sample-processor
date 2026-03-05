using System;

namespace SensorAnalysis.Domain.Entities;

public class SensorSample
{
    public string SensorId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public double? Temperature { get; private set; }
    public double? Humidity { get; private set; }
    public double? DewPoint { get; private set; }

    private SensorSample() { }

    public static SensorSample Create(
        string sensorId,
        string type,
        DateTime timestamp,
        double? temperature,
        double? humidity,
        double? dewPoint)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new ArgumentException("SensorId não pode ser vazio", nameof(sensorId));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type não pode ser vazio", nameof(type));

        return new SensorSample
        {
            SensorId = sensorId,
            Type = type,
            Timestamp = timestamp,
            Temperature = temperature,
            Humidity = humidity,
            DewPoint = dewPoint
        };
    }

    public bool IsInvalid()
    {
        return !Temperature.HasValue || !Humidity.HasValue || !DewPoint.HasValue;
    }

    public bool IsValid() => !IsInvalid();
    public bool HasValidTemperature() => Temperature.HasValue;
    public bool HasValidHumidity() => Humidity.HasValue;
    public bool HasValidDewPoint() => DewPoint.HasValue;
}
