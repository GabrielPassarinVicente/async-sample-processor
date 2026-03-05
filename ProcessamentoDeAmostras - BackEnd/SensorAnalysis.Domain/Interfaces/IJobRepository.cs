using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Domain.Interfaces;

public interface IJobRepository
{
    Task<JobStatus?> GetByIdAsync(string jobId);
    Task AddAsync(JobStatus job);
    Task UpdateAsync(JobStatus job);
}
