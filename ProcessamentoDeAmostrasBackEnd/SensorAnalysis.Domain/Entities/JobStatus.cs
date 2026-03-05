using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Domain.Entities;

public class JobStatus
{
    public string JobId { get; private set; }
    public JobState State { get; private set; }
    public int TotalSamples { get; private set; }
    public int ProcessedSamples { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<object> Results { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private JobStatus(string jobId, int totalSamples)
    {
        JobId = jobId;
        TotalSamples = totalSamples;
        ProcessedSamples = 0;
        State = JobState.Processing;
        Results = new List<object>();
        CreatedAt = DateTime.UtcNow;
    }

    public static JobStatus Create(string jobId, int totalSamples)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("JobId cannot be null or empty", nameof(jobId));

        if (totalSamples <= 0)
            throw new ArgumentException("TotalSamples must be greater than zero", nameof(totalSamples));

        return new JobStatus(jobId, totalSamples);
    }

    public void IncrementProgress()
    {
        if (State != JobState.Processing)
            return;

        if (ProcessedSamples < TotalSamples)
            ProcessedSamples++;
    }

    public void Complete(List<object> results)
    {
        if (State != JobState.Processing)
            return;

        State = JobState.Completed;
        Results = results ?? new List<object>();
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        if (State == JobState.Completed)
            throw new InvalidOperationException("A completed job cannot be marked as failed.");

        State = JobState.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public bool IsCompleted => State == JobState.Completed;
    public bool IsFailed => State == JobState.Failed;
    public bool IsProcessing => State == JobState.Processing;

    public int GetProgressPercentage()
    {
        if (TotalSamples == 0) return 0;
        return (ProcessedSamples * 100) / TotalSamples;
    }

    public bool CanDownload() => IsCompleted && Results.Count > 0;
}
