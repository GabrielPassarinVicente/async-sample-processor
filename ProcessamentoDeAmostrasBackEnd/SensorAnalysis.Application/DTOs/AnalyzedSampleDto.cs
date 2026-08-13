namespace SensorAnalysis.Application.DTOs;

public class AnalyzedSampleDto
{
    public string SensorId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? DewPoint { get; set; }
    public AnalysisDto Analysis { get; set; } = new();
}

public class AnalysisDto
{
    public MetricDto Temperature { get; set; } = new();
    public MetricDto Humidity { get; set; } = new();
    public MetricDto DewPoint { get; set; } = new();
    public AnomalyDto Anomaly { get; set; } = new();
}

public class MetricDto
{
    public string Status { get; set; } = "normal";
    public string? LimitType { get; set; }
    public double? ThresholdValue { get; set; }
}

public class AnomalyDto
{
    public string Status { get; set; } = "normal";
}
