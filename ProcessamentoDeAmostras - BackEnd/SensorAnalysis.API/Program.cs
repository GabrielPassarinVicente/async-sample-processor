using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Application.UseCases;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using SensorAnalysis.Infrastructure.Algorithms;
using SensorAnalysis.Infrastructure.Messaging;
using SensorAnalysis.Infrastructure.Persistence;

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

builder.Services.Configure<SensorAnalysis.Infrastructure.Configuration.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SensorEvaluator>();

builder.Services.AddSingleton<ProcessSensorFileUseCase>();
builder.Services.AddSingleton<DownloadResultsUseCase>();
builder.Services.AddSingleton<SensorAnalysis.Application.Services.SensorFileParser>();

builder.Services.AddSingleton<IAnomalyDetector, IqrAnomalyDetector>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok());

app.Run();
