using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Domain.ValueObjects;

public sealed class SampleAnalysisResult
{
    public string SensorId { get; }
    public string SensorType { get; }
    public DateTime Timestamp { get; }
    public double? Temperature { get; }
    public double? Humidity { get; }
    public double? DewPoint { get; }
    public SampleAnalysis Analysis { get; }

    private SampleAnalysisResult(
        string sensorId,
        string sensorType,
        DateTime timestamp,
        double? temperature,
        double? humidity,
        double? dewPoint,
        SampleAnalysis analysis)
    {
        SensorId = sensorId;
        SensorType = sensorType;
        Timestamp = timestamp;
        Temperature = temperature;
        Humidity = humidity;
        DewPoint = dewPoint;
        Analysis = analysis;
    }

    public static SampleAnalysisResult Create(SensorSample sample, SampleAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(analysis);

        return new SampleAnalysisResult(
            sample.SensorId,
            sample.Type,
            sample.Timestamp,
            sample.Temperature,
            sample.Humidity,
            sample.DewPoint,
            analysis);
    }
}
