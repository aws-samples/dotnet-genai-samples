FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["grpc-hello-service/GrpcHelloService.csproj", "grpc-hello-service/"]
RUN dotnet restore "grpc-hello-service/GrpcHelloService.csproj"
COPY . .
WORKDIR "/src/grpc-hello-service"
RUN dotnet build "GrpcHelloService.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "GrpcHelloService.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GrpcHelloService.dll"]
