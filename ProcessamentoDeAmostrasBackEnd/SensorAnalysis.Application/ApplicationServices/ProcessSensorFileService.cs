using SensorAnalysis.Application.Services;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.ApplicationServices;

// Chamado por: SensorController (Apresentação) via StartAsync()
// Responsabilidade: orquestração pura — sem regras de negócio
public class ProcessSensorFileService
{
    private readonly SensorFileParser _fileParser;
    private readonly IJobRepository _jobRepository;
    private readonly IJobProcessingQueue _jobProcessingQueue;

    public ProcessSensorFileService(
        SensorFileParser fileParser,
        IJobRepository jobRepository,
        IJobProcessingQueue jobProcessingQueue)
    {
        _fileParser = fileParser;
        _jobRepository = jobRepository;
        _jobProcessingQueue = jobProcessingQueue;
    }

    // Chamado por: SensorController.UploadFile()
    // Enfileira o job e retorna o jobId imediatamente; o processamento ocorre
    // em background via JobProcessingBackgroundService (Infrastructure)
    public async Task<Result<string>> StartAsync(Stream fileStream)
    {
        var parseResult = await _fileParser.ParseAsync(fileStream);

        if (parseResult.IsFailure)
            return Result<string>.Failure(parseResult.Error!);

        var samples = parseResult.Value!;
        var jobId = Guid.NewGuid().ToString();

        var job = JobStatus.Create(jobId, samples.Count);
        await _jobRepository.AddAsync(job);

        await _jobProcessingQueue.EnqueueAsync(new ProcessingJob(job, samples));

        return Result<string>.Success(jobId);
    }
}
