namespace SensorAnalysis.Domain.ValueObjects;

public sealed class MetricThresholds
{
    public double? AlertMin { get; }
    public double? AlertMax { get; }
    public double? CriticalMin { get; }
    public double? CriticalMax { get; }

    private MetricThresholds(double? alertMin, double? alertMax, double? criticalMin, double? criticalMax)
    {
        if (alertMin.HasValue && criticalMin.HasValue && criticalMin > alertMin)
            throw new ArgumentException("Critical min must be lower than alert min");

        if (alertMax.HasValue && criticalMax.HasValue && criticalMax < alertMax)
            throw new ArgumentException("Critical max must be higher than alert max");

        AlertMin = alertMin;
        AlertMax = alertMax;
        CriticalMin = criticalMin;
        CriticalMax = criticalMax;
    }

    public static MetricThresholds Create(double? alertMin, double? alertMax, double? criticalMin, double? criticalMax)
    {
        return new MetricThresholds(alertMin, alertMax, criticalMin, criticalMax);
    }

    public static MetricThresholds Temperature => Create(15.0, 30.0, 10.0, 35.0);
    public static MetricThresholds Humidity => Create(40.0, 70.0, 30.0, 80.0);
    public static MetricThresholds DewPoint => Create(null, 22.0, null, 25.0);
}
