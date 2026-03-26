using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Application.Services;
using SensorAnalysis.Domain.Services;
using SensorAnalysis.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Domain Services
builder.Services.AddSingleton<SensorEvaluator>();

// Application Services
builder.Services.AddSingleton<SensorFileParser>();
builder.Services.AddSingleton<ProcessSensorFileService>();
builder.Services.AddSingleton<DownloadResultsService>();
builder.Services.AddSingleton<GetJobStatusService>();

// Infrastructure (implementações internas registradas via extension method)
builder.Services.Configure<SensorAnalysis.Infrastructure.Configuration.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok());

app.Run();
