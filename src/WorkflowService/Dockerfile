FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo
COPY src/WorkflowService/WorkflowService.csproj src/WorkflowService/
RUN dotnet restore src/WorkflowService/WorkflowService.csproj
COPY src/WorkflowService/ src/WorkflowService/
RUN dotnet publish src/WorkflowService/WorkflowService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorkflowService.dll"]
