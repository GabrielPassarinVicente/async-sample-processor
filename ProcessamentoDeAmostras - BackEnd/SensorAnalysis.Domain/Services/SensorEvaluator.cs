using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Domain.Services;

public class SensorEvaluator
{
    public SampleAnalysis Evaluate(SensorSample sample)
    {
        if (sample.IsInvalid())
        {
            var invalidAnalysis = SampleAnalysis.Create(
                MetricAnalysis.CreateInvalid(),
                MetricAnalysis.CreateInvalid(),
                MetricAnalysis.CreateInvalid()
            );
            invalidAnalysis.MarkAsInvalid();
            return invalidAnalysis;
        }

        var temperatureAnalysis = EvaluateMetric(
            sample.Temperature!.Value,
            MetricThresholds.Temperature);

        var humidityAnalysis = EvaluateMetric(
            sample.Humidity!.Value,
            MetricThresholds.Humidity);

        var dewPointAnalysis = EvaluateMetric(
            sample.DewPoint!.Value,
            MetricThresholds.DewPoint);

        return SampleAnalysis.Create(temperatureAnalysis, humidityAnalysis, dewPointAnalysis);
    }

    private MetricAnalysis EvaluateMetric(double value, MetricThresholds thresholds)
    {
        if (thresholds.CriticalMax.HasValue && value > thresholds.CriticalMax.Value)
            return MetricAnalysis.CreateCritical(Enums.LimitType.Max, thresholds.CriticalMax.Value);

        if (thresholds.CriticalMin.HasValue && value < thresholds.CriticalMin.Value)
            return MetricAnalysis.CreateCritical(Enums.LimitType.Min, thresholds.CriticalMin.Value);

        if (thresholds.AlertMax.HasValue && value > thresholds.AlertMax.Value)
            return MetricAnalysis.CreateAlert(Enums.LimitType.Max, thresholds.AlertMax.Value);

        if (thresholds.AlertMin.HasValue && value < thresholds.AlertMin.Value)
            return MetricAnalysis.CreateAlert(Enums.LimitType.Min, thresholds.AlertMin.Value);

        return MetricAnalysis.CreateNormal();
    }
}

