using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SensorAnalysis.Application.ApplicationServices;

namespace SensorAnalysis.API.Controllers;

// Camada de Apresentação: recebe requisições HTTP, mapeia para DTOs/comandos e delega
// Regra: zero lógica de negócio aqui — toda decisão é tomada pela camada de Aplicação
[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ProcessSensorFileService _processService;
    private readonly DownloadResultsService _downloadService;
    private readonly GetJobStatusService _getJobStatusService;
    private readonly ILogger<SensorController> _logger;

    public SensorController(
        ProcessSensorFileService processService,
        DownloadResultsService downloadService,
        GetJobStatusService getJobStatusService,
        ILogger<SensorController> logger)
    {
        _processService = processService;
        _downloadService = downloadService;
        _getJobStatusService = getJobStatusService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        _logger.LogInformation("📤 Upload request received");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("⚠️ Invalid or empty file");
            return BadRequest(new { error = "Arquivo inválido ou vazio" });
        }

        _logger.LogInformation("📁 File: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _processService.StartAsync(stream);

            if (result.IsFailure)
            {
                _logger.LogError("❌ Processing failed: {Error}", result.Error!.Message);
                return BadRequest(new { error = result.Error!.Message });
            }

            _logger.LogInformation("🚀 Job started: {JobId}", result.Value);

            return Accepted(new
            {
                jobId = result.Value,
                message = "Processamento iniciado em background."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during upload");
            return StatusCode(500, new { error = "Erro interno ao processar arquivo" });
        }
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        var result = await _getJobStatusService.ExecuteAsync(jobId);

        if (result.IsFailure)
            return NotFound(new { error = result.Error!.Message });

        return Ok(result.Value);
    }

    [HttpGet("download/{jobId}")]
    public async Task<IActionResult> DownloadResults(string jobId)
    {
        var result = await _downloadService.ExecuteAsync(jobId);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "JOB_NOT_FOUND" => NotFound(new { error = result.Error.Message, code = result.Error.Code }),
                "JOB_NOT_COMPLETED" => BadRequest(new { error = result.Error.Message, code = result.Error.Code }),
                "JOB_FAILED" => BadRequest(new { error = result.Error.Message, code = result.Error.Code }),
                "INVALID_JOB_ID" => BadRequest(new { error = result.Error.Message, code = result.Error.Code }),
                "NO_RESULTS" => NotFound(new { error = result.Error.Message, code = result.Error.Code }),
                _ => StatusCode(500, new { error = result.Error.Message, code = result.Error.Code })
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var json = JsonSerializer.Serialize(result.Value, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"sensor_analysis_{jobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        return File(bytes, "application/json", fileName);
    }
}
