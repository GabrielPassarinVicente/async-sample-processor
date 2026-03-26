using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Application.Mappers;

public static class JobMapper
{
    public static JobStatusDto ToDto(JobStatus job)
    {
        return new JobStatusDto
        {
            JobId = job.JobId,
            Status = GetStatusString(job),
            IsCompleted = job.IsCompleted,
            IsFailed = job.IsFailed,
            ErrorMessage = job.ErrorMessage,
            TotalSamples = job.TotalSamples,
            ProcessedSamples = job.ProcessedSamples,
            Results = job.Results.Select(SensorMapper.ToDto).ToList()
        };
    }

    public static DownloadResultDto ToDownloadDto(JobStatus job)
    {
        return new DownloadResultDto
        {
            JobId = job.JobId,
            TotalSamples = job.TotalSamples,
            ProcessedSamples = job.ProcessedSamples,
            CompletedAt = job.CompletedAt ?? DateTime.UtcNow,
            Results = job.Results.Select(SensorMapper.ToDto).ToList()
        };
    }

    private static string GetStatusString(JobStatus job)
    {
        return job.State switch
        {
            JobState.Processing => "processing",
            JobState.Completed => "completed",
            JobState.Failed => "failed",
            _ => "unknown"
        };
    }
}

