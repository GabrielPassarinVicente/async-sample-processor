using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Domain.Entities;

public class JobStatus
{
    private readonly List<SampleAnalysisResult> _results = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public string JobId { get; private set; }
    public JobState State { get; private set; }
    public int TotalSamples { get; private set; }
    public int ProcessedSamples { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<SampleAnalysisResult> Results => _results.AsReadOnly();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private JobStatus(string jobId, int totalSamples)
    {
        JobId = jobId;
        TotalSamples = totalSamples;
        ProcessedSamples = 0;
        State = JobState.Processing;
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

    public void Complete(IReadOnlyList<SampleAnalysisResult> results)
    {
        if (State != JobState.Processing)
            return;

        _results.AddRange(results);

        foreach (var result in results)
        {
            if (result.Analysis.IsCritical() || result.Analysis.IsAnomaly())
            {
                var reason = result.Analysis.IsCritical() ? "critical" : "anomaly";
                _domainEvents.Add(SensorAnomalyDetected.Create(result.SensorId, result.Timestamp, reason));
            }
        }

        State = JobState.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        if (State == JobState.Completed)
            throw new InvalidJobOperationException("A completed job cannot be marked as failed.");

        State = JobState.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool IsCompleted => State == JobState.Completed;
    public bool IsFailed => State == JobState.Failed;
    public bool IsProcessing => State == JobState.Processing;

    public int GetProgressPercentage()
    {
        if (TotalSamples == 0) return 0;
        return (ProcessedSamples * 100) / TotalSamples;
    }

    public bool CanDownload() => IsCompleted && _results.Count > 0;
}
