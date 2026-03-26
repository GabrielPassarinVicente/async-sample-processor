using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Application.Mappers;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.ApplicationServices;

public class GetJobStatusService
{
    private readonly IJobRepository _jobRepository;

    public GetJobStatusService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<JobStatusDto>> ExecuteAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return Result<JobStatusDto>.Failure(ApplicationErrors.InvalidJobId);

        var job = await _jobRepository.GetByIdAsync(jobId);

        if (job == null)
            return Result<JobStatusDto>.Failure(ApplicationErrors.JobNotFound);

        return Result<JobStatusDto>.Success(JobMapper.ToDto(job));
    }
}
