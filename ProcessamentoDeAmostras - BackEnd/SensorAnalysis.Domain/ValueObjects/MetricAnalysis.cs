using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Domain.ValueObjects;

public class MetricAnalysis
{
    public StatusLevel Status { get; private set; }
    public LimitType? LimitType { get; private set; }
    public double? ThresholdValue { get; private set; }

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

