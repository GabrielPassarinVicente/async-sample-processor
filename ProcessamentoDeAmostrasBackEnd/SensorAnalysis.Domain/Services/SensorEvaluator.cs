using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Domain.Services;

// Domain Service: avalia as métricas de uma amostra contra os limiares configurados
// Chamado por: JobStatus.Process() (Domain) — passado como parâmetro ao Aggregate Root
public sealed class SensorEvaluator
{
    public SampleAnalysis Evaluate(SensorSample sample)
    {
        if (sample.IsInvalid())
            return SampleAnalysis.CreateInvalid();

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
            return MetricAnalysis.CreateCritical(LimitType.Max, thresholds.CriticalMax.Value);

        if (thresholds.CriticalMin.HasValue && value < thresholds.CriticalMin.Value)
            return MetricAnalysis.CreateCritical(LimitType.Min, thresholds.CriticalMin.Value);

        if (thresholds.AlertMax.HasValue && value > thresholds.AlertMax.Value)
            return MetricAnalysis.CreateAlert(LimitType.Max, thresholds.AlertMax.Value);

        if (thresholds.AlertMin.HasValue && value < thresholds.AlertMin.Value)
            return MetricAnalysis.CreateAlert(LimitType.Min, thresholds.AlertMin.Value);

        return MetricAnalysis.CreateNormal();
    }
}

