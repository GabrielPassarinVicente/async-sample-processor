using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Domain.ValueObjects;

public class SampleAnalysis
{
    public MetricAnalysis Temperature { get; private set; }
    public MetricAnalysis Humidity { get; private set; }
    public MetricAnalysis DewPoint { get; private set; }
    public StatusLevel AnomalyStatus { get; private set; }

    private SampleAnalysis()
    {
        Temperature = MetricAnalysis.CreateNormal();
        Humidity = MetricAnalysis.CreateNormal();
        DewPoint = MetricAnalysis.CreateNormal();
        AnomalyStatus = StatusLevel.Normal;
    }

    public static SampleAnalysis Create(
        MetricAnalysis temperature,
        MetricAnalysis humidity,
        MetricAnalysis dewPoint)
    {
        return new SampleAnalysis
        {
            Temperature = temperature,
            Humidity = humidity,
            DewPoint = dewPoint
        };
    }

    public void MarkAsAnomaly()
    {
        AnomalyStatus = StatusLevel.Anomaly;
    }

    public void MarkAsInvalid()
    {
        AnomalyStatus = StatusLevel.Invalid;
    }

    public bool IsCritical()
    {
        return Temperature.Status == StatusLevel.Critical
            || Humidity.Status == StatusLevel.Critical
            || DewPoint.Status == StatusLevel.Critical;
    }

    public bool IsAnomaly()
    {
        return AnomalyStatus == StatusLevel.Anomaly;
    }
}

