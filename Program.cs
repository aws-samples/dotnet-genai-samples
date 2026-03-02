using GrpcHelloService.Interceptors;
using GrpcHelloService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var grpcPort = int.TryParse(Environment.GetEnvironmentVariable("GRPC_PORT"), out var port) ? port : 8080;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
});
builder.Services.AddGrpcHealthChecks()
    .AddAsyncCheck("live", () =>
        Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()));

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapGrpcHealthChecksService();

Console.WriteLine($"gRPC service listening on http://0.0.0.0:{grpcPort}");

app.Run();
