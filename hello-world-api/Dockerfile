FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY grpc-hello-service/protos/greet.proto protos/greet.proto
COPY ["hello-world-api/HelloWorldApi.csproj", "hello-world-api/"]
RUN dotnet restore "hello-world-api/HelloWorldApi.csproj"
COPY . .
WORKDIR "/src/hello-world-api"
RUN dotnet build "HelloWorldApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "HelloWorldApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HelloWorldApi.dll"]
