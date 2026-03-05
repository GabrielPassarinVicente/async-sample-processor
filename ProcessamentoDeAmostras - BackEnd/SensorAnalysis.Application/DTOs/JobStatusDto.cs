namespace SensorAnalysis.Application.DTOs;

public class JobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "processing";
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalSamples { get; set; }
    public int ProcessedSamples { get; set; }
    public List<AnalyzedSampleDto>? Results { get; set; }
}
