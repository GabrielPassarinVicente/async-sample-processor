using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Domain.ValueObjects;

public sealed class SampleAnalysis
{
    public MetricAnalysis Temperature { get; }
    public MetricAnalysis Humidity { get; }
    public MetricAnalysis DewPoint { get; }
    public StatusLevel AnomalyStatus { get; }

    private SampleAnalysis(
        MetricAnalysis temperature,
        MetricAnalysis humidity,
        MetricAnalysis dewPoint,
        StatusLevel anomalyStatus)
    {
        Temperature = temperature;
        Humidity = humidity;
        DewPoint = dewPoint;
        AnomalyStatus = anomalyStatus;
    }

    public static SampleAnalysis CreateInvalid() => new(
        MetricAnalysis.CreateInvalid(),
        MetricAnalysis.CreateInvalid(),
        MetricAnalysis.CreateInvalid(),
        StatusLevel.Invalid);

    public static SampleAnalysis Create(
        MetricAnalysis temperature,
        MetricAnalysis humidity,
        MetricAnalysis dewPoint) => new(temperature, humidity, dewPoint, StatusLevel.Normal);

    public SampleAnalysis AsAnomaly() => new(Temperature, Humidity, DewPoint, StatusLevel.Anomaly);

    public bool IsCritical() =>
        Temperature.Status == StatusLevel.Critical ||
        Humidity.Status == StatusLevel.Critical ||
        DewPoint.Status == StatusLevel.Critical;

    public bool IsAnomaly() => AnomalyStatus == StatusLevel.Anomaly;
    public bool IsInvalid() => AnomalyStatus == StatusLevel.Invalid;
}

