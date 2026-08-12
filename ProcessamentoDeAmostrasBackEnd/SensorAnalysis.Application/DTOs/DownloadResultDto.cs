namespace SensorAnalysis.Application.DTOs;

public class DownloadResultDto
{
    public string JobId { get; set; } = string.Empty;
    public int TotalSamples { get; set; }
    public int ProcessedSamples { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<AnalyzedSampleDto> Results { get; set; } = new();
}
