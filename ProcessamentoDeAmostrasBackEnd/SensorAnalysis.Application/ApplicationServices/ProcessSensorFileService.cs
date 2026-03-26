using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Application.Services;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;

namespace SensorAnalysis.Application.ApplicationServices;

// Chamado por: SensorController (Apresentação) via StartAsync()
// Responsabilidade: orquestração pura — sem regras de negócio
public class ProcessSensorFileService
{
    private readonly SensorFileParser _fileParser;
    private readonly SensorEvaluator _evaluator;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IJobRepository _jobRepository;

    public ProcessSensorFileService(
        SensorFileParser fileParser,
        SensorEvaluator evaluator,
        IAnomalyDetector anomalyDetector,
        IMessagePublisher messagePublisher,
        IJobRepository jobRepository)
    {
        _fileParser = fileParser;
        _evaluator = evaluator;
        _anomalyDetector = anomalyDetector;
        _messagePublisher = messagePublisher;
        _jobRepository = jobRepository;
    }

    // Chamado por: SensorController.UploadFile()
    // Retorna o jobId imediatamente; o processamento continua em background
    public async Task<Result<string>> StartAsync(Stream fileStream)
    {
        var parseResult = await _fileParser.ParseAsync(fileStream);

        if (parseResult.IsFailure)
            return Result<string>.Failure(parseResult.Error!);

        var samples = parseResult.Value!;
        var jobId = Guid.NewGuid().ToString();

        // Passo 1: Cria o Agregado e persiste o estado inicial
        var job = JobStatus.Create(jobId, samples.Count);
        await _jobRepository.AddAsync(job);

        _ = ProcessInBackgroundAsync(job, samples);

        return Result<string>.Success(jobId);
    }

    private async Task ProcessInBackgroundAsync(JobStatus job, List<SensorSample> samples)
    {
        try
        {
            await ProcessAsync(job, samples);
        }
        catch (Exception ex)
        {
            job.MarkAsFailed(ex.Message);
            await _jobRepository.UpdateAsync(job);
        }
    }

    // O fluxo DDD de 3 passos: Load → Call domain method → Save
    private async Task ProcessAsync(JobStatus job, List<SensorSample> samples)
    {
        // Passo 2: Delega toda a lógica de negócio ao Aggregate Root
        // O Agregado encapsula: avaliação, detecção de anomalias, progresso e Domain Events
        job.Process(samples, _evaluator, _anomalyDetector);

        // Aplicação: despacha os Domain Events gerados pelo Agregado via messaging
        foreach (var domainEvent in job.DomainEvents.OfType<SensorAnomalyDetected>())
            await _messagePublisher.PublishAsync(domainEvent);

        job.ClearDomainEvents();

        // Passo 3: Persiste o novo estado do Agregado
        await _jobRepository.UpdateAsync(job);
    }
}
