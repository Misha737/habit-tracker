FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

COPY Practice4.ModularMonolith.sln ./
COPY src/Modules.Core.Domain/Modules.Core.Domain.csproj          src/Modules.Core.Domain/
COPY src/Modules.Core.Application/Modules.Core.Application.csproj src/Modules.Core.Application/
COPY src/Modules.Core.Infrastructure/Modules.Core.Infrastructure.csproj src/Modules.Core.Infrastructure/
COPY src/Api/Api.csproj                                            src/Api/
COPY src/Shared/Shared.csproj                                      src/Shared/
COPY tests/Modules.Core.Tests/Modules.Core.Tests.csproj           tests/Modules.Core.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
