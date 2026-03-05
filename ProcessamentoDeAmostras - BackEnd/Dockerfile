FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SensorAnalysis.API/SensorAnalysis.API.csproj", "SensorAnalysis.API/"]
COPY ["SensorAnalysis.Application/SensorAnalysis.Application.csproj", "SensorAnalysis.Application/"]
COPY ["SensorAnalysis.Domain/SensorAnalysis.Domain.csproj", "SensorAnalysis.Domain/"]
COPY ["SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj", "SensorAnalysis.Infrastructure/"]
RUN dotnet restore "SensorAnalysis.API/SensorAnalysis.API.csproj"

COPY . .
WORKDIR "/src/SensorAnalysis.API"
RUN dotnet publish "SensorAnalysis.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SensorAnalysis.API.dll"]