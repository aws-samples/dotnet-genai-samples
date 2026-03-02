using Grpc.Net.Client;
using GrpcHelloService;

var builder = WebApplication.CreateBuilder(args);

var grpcAddress = Environment.GetEnvironmentVariable("GRPC_SERVICE_URL") ?? "http://grpc-hello-service:8080";

builder.Services.AddSingleton(_ =>
{
    var channel = GrpcChannel.ForAddress(grpcAddress);
    return new Greeter.GreeterClient(channel);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World API - gRPC Client");

app.MapGet("/hello/{name}", async (string name, Greeter.GreeterClient client) =>
{
    try
    {
        var reply = await client.SayHelloAsync(new HelloRequest { Name = name });
        return Results.Ok(new { message = reply.Message });
    }
    catch (Grpc.Core.RpcException ex)
    {
        return Results.Problem(
            detail: ex.Status.Detail,
            statusCode: ex.StatusCode == Grpc.Core.StatusCode.Internal ? 500 : 502,
            title: $"gRPC error: {ex.StatusCode}");
    }
});

Console.WriteLine($"REST API listening, forwarding gRPC calls to {grpcAddress}");

app.Run();
