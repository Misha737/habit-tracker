FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

COPY src/Modules.Core.Domain/Modules.Core.Domain.csproj          src/Modules.Core.Domain/
COPY src/Modules.Core.Application/Modules.Core.Application.csproj src/Modules.Core.Application/
COPY src/Modules.Core.Infrastructure/Modules.Core.Infrastructure.csproj src/Modules.Core.Infrastructure/
COPY src/Shared/Shared.csproj                                      src/Shared/
COPY src/Api/Api.csproj                                            src/Api/
RUN dotnet restore src/Api/Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
