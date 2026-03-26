using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Application.Mappers;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.ApplicationServices;

public class DownloadResultsService
{
    private readonly IJobRepository _jobRepository;

    public DownloadResultsService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<DownloadResultDto>> ExecuteAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return Result<DownloadResultDto>.Failure(ApplicationErrors.InvalidJobId);

        var job = await _jobRepository.GetByIdAsync(jobId);

        if (job == null)
            return Result<DownloadResultDto>.Failure(ApplicationErrors.JobNotFound);

        if (job.IsFailed)
            return Result<DownloadResultDto>.Failure(
                ApplicationErrors.JobFailed(job.ErrorMessage ?? "Unknown error"));

        if (!job.IsCompleted)
            return Result<DownloadResultDto>.Failure(
                ApplicationErrors.JobNotCompleted(job.State.ToString()));

        if (!job.CanDownload())
            return Result<DownloadResultDto>.Failure(ApplicationErrors.NoResultsAvailable);

        var downloadDto = JobMapper.ToDownloadDto(job);

        return Result<DownloadResultDto>.Success(downloadDto);
    }
}
