using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class FakeJobRepository : IJobRepository
{
    private readonly Dictionary<string, JobStatus> _jobs = new();

    public JobStatus? Stored(string jobId) => _jobs.GetValueOrDefault(jobId);

    public Task<JobStatus?> GetByIdAsync(string jobId)
        => Task.FromResult(_jobs.GetValueOrDefault(jobId));

    public Task AddAsync(JobStatus job)
    {
        _jobs[job.JobId] = job;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(JobStatus job)
    {
        _jobs[job.JobId] = job;
        return Task.CompletedTask;
    }
}
