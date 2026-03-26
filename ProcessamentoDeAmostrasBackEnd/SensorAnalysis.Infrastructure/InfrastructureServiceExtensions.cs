using Microsoft.Extensions.DependencyInjection;
using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Infrastructure.Algorithms;
using SensorAnalysis.Infrastructure.Messaging;
using SensorAnalysis.Infrastructure.Persistence;

namespace SensorAnalysis.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IAnomalyDetector, IqrAnomalyDetector>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }
}
