# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy proto from parent
COPY grpc-hello-service/protos/greet.proto protos/greet.proto

# Copy project file and restore
COPY hello-world-api/HelloWorldApi.csproj hello-world-api/
RUN dotnet restore hello-world-api/HelloWorldApi.csproj

# Copy source and publish
COPY hello-world-api/ hello-world-api/
RUN dotnet publish hello-world-api/HelloWorldApi.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "HelloWorldApi.dll"]
