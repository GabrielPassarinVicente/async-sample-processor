using Microsoft.Extensions.DependencyInjection;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Infrastructure.Algorithms;
using SensorAnalysis.Infrastructure.Messaging;
using SensorAnalysis.Infrastructure.Persistence;
using SensorAnalysis.Infrastructure.Processing;

namespace SensorAnalysis.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IAnomalyDetector, IqrAnomalyDetector>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton<IJobProcessingQueue, ChannelJobProcessingQueue>();
        services.AddHostedService<JobProcessingBackgroundService>();

        return services;
    }
}
