using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Application.Mappers;
using SensorAnalysis.Application.Services;
using SensorAnalysis.Application.UseCases;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ProcessSensorFileUseCase _processUseCase;
    private readonly DownloadResultsUseCase _downloadUseCase;
    private readonly SensorFileParser _fileParser;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<SensorController> _logger;

    public SensorController(
        ProcessSensorFileUseCase processUseCase,
        DownloadResultsUseCase downloadUseCase,
        SensorFileParser fileParser,
        IJobRepository jobRepository,
        ILogger<SensorController> logger)
    {
        _processUseCase = processUseCase;
        _downloadUseCase = downloadUseCase;
        _fileParser = fileParser;
        _jobRepository = jobRepository;
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
            var parseResult = await _fileParser.ParseAsync(stream);

            if (parseResult.IsFailure)
            {
                _logger.LogError("❌ Parse failed: {Error}", parseResult.Error!.Message);
                return BadRequest(new { error = parseResult.Error!.Message });
            }

            _logger.LogInformation("✅ Parse successful: {Count} samples", parseResult.Value!.Count);

            var result = await _processUseCase.StartAsync(parseResult.Value!);

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
        var job = await _jobRepository.GetByIdAsync(jobId);

        if (job == null)
        {
            return NotFound(new { error = "Job não encontrado" });
        }

        var statusDto = JobMapper.ToDto(job);
        return Ok(statusDto);
    }

    [HttpGet("download/{jobId}")]
    public async Task<IActionResult> DownloadResults(string jobId)
    {
        var result = await _downloadUseCase.ExecuteAsync(jobId);

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
