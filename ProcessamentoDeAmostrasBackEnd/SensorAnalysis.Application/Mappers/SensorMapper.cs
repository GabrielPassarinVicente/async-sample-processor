using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Application.Mappers;

public static class SensorMapper
{
    public static AnalyzedSampleDto ToDto(SampleAnalysisResult result)
    {
        return new AnalyzedSampleDto
        {
            SensorId = result.SensorId,
            Type = result.SensorType,
            Timestamp = result.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Temperature = result.Temperature,
            Humidity = result.Humidity,
            DewPoint = result.DewPoint,
            Analysis = new AnalysisDto
            {
                Temperature = MapMetric(result.Analysis.Temperature),
                Humidity = MapMetric(result.Analysis.Humidity),
                DewPoint = MapMetric(result.Analysis.DewPoint),
                Anomaly = new AnomalyDto
                {
                    Status = result.Analysis.AnomalyStatus.ToString().ToLowerInvariant()
                }
            }
        };
    }

    private static MetricDto MapMetric(MetricAnalysis metric)
    {
        return new MetricDto
        {
            Status = metric.Status.ToString().ToLowerInvariant(),
            LimitType = metric.LimitType?.ToString().ToLowerInvariant(),
            ThresholdValue = metric.ThresholdValue
        };
    }
}
