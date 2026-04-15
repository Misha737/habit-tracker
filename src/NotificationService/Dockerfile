FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo
COPY src/Shared/Shared.csproj                                              src/Shared/
COPY src/NotificationService/NotificationService.csproj  src/NotificationService/
RUN dotnet restore src/NotificationService/NotificationService.csproj
COPY src/Shared/        src/Shared/
COPY src/NotificationService/ src/NotificationService/
RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.dll"]
