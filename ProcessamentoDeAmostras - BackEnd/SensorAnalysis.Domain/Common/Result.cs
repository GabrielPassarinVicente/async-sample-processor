namespace SensorAnalysis.Domain.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error EmptyFile = new("EMPTY_FILE", "O arquivo está vazio");
    public static readonly Error InvalidFormat = new("INVALID_FORMAT", "Formato de arquivo inválido");
    public static readonly Error ProcessingFailed = new("PROCESSING_FAILED", "Falha ao processar arquivo");
    public static readonly Error InvalidJobId = new("INVALID_JOB_ID", "ID do job inválido");
    public static readonly Error JobNotFound = new("JOB_NOT_FOUND", "Job não encontrado");
    public static readonly Error NoResultsAvailable = new("NO_RESULTS", "Nenhum resultado disponível para este job");

    public static Error JobNotCompleted(string currentStatus) =>
        new("JOB_NOT_COMPLETED", $"Job ainda não foi completado. Status atual: {currentStatus}");

    public static Error JobFailed(string errorMessage) =>
        new("JOB_FAILED", $"Job falhou durante o processamento: {errorMessage}");
}
