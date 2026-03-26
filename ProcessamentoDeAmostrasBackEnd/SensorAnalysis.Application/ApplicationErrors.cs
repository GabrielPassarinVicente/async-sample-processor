using SensorAnalysis.Domain.Common;

namespace SensorAnalysis.Application;

public static class ApplicationErrors
{
    public static readonly Error EmptyFile = new("EMPTY_FILE", "O arquivo está vazio");
    public static readonly Error InvalidFormat = new("INVALID_FORMAT", "Formato de arquivo inválido");
    public static readonly Error InvalidJobId = new("INVALID_JOB_ID", "ID do job inválido");
    public static readonly Error JobNotFound = new("JOB_NOT_FOUND", "Job não encontrado");
    public static readonly Error NoResultsAvailable = new("NO_RESULTS", "Nenhum resultado disponível para este job");

    public static Error JobNotCompleted(string currentStatus) =>
        new("JOB_NOT_COMPLETED", $"Job ainda não foi completado. Status atual: {currentStatus}");

    public static Error JobFailed(string errorMessage) =>
        new("JOB_FAILED", $"Job falhou durante o processamento: {errorMessage}");
}
