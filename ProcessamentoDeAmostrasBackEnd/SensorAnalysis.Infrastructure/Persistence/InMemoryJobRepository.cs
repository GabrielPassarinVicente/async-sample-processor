using System.Collections.Concurrent;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Persistence;

internal sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<string, JobStatus> _jobs = new();

    public Task<JobStatus?> GetByIdAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task AddAsync(JobStatus job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!_jobs.TryAdd(job.JobId, job))
            throw new InvalidOperationException($"Job '{job.JobId}' already exists.");

        return Task.CompletedTask;
    }

    public Task UpdateAsync(JobStatus job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!_jobs.ContainsKey(job.JobId))
            throw new InvalidOperationException($"Job '{job.JobId}' not found for update.");

        _jobs[job.JobId] = job;
        return Task.CompletedTask;
    }
}
