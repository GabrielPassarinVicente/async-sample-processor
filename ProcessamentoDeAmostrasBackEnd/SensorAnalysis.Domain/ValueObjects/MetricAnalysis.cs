using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Domain.ValueObjects;

public sealed class MetricAnalysis
{
    public StatusLevel Status { get; init; }
    public LimitType? LimitType { get; init; }
    public double? ThresholdValue { get; init; }

    private MetricAnalysis() { }

    public static MetricAnalysis CreateNormal()
        => new() { Status = StatusLevel.Normal };

    public static MetricAnalysis CreateAlert(LimitType limitType, double threshold)
        => new()
        {
            Status = StatusLevel.Alert,
            LimitType = limitType,
            ThresholdValue = threshold
        };

    public static MetricAnalysis CreateCritical(LimitType limitType, double threshold)
        => new()
        {
            Status = StatusLevel.Critical,
            LimitType = limitType,
            ThresholdValue = threshold
        };

    public static MetricAnalysis CreateInvalid()
        => new() { Status = StatusLevel.Invalid };
}

