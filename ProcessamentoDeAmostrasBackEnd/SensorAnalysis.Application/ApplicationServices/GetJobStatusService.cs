using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Application.Mappers;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.ApplicationServices;

// Chamado por: SensorController (Apresentação) via ExecuteAsync()
// Responsabilidade: orquestração pura — sem regras de negócio
public class GetJobStatusService
{
    private readonly IJobRepository _jobRepository;

    public GetJobStatusService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    // Passo 1: Carrega o Agregado pelo Repository
    // Passo 2: Delega ao Mapper a projeção do estado para o DTO
    // Sem Passo 3 — operação de leitura, nenhum estado é alterado
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
