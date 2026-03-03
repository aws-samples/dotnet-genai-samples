using Grpc.Core;
using GrpcHelloService;

namespace GrpcHelloService.Services;


public class GreeterService : Greeter.GreeterBase
{
    private static int _requestCount;
    private static readonly Random _random = new();

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name must not be empty"));
        }

        var count = Interlocked.Increment(ref _requestCount);

        // Every 5th request: add a random delay between 2-5 seconds
        if (count % 5 == 0)
        {
            var delayMs = _random.Next(2000, 5001);
            Console.WriteLine($"[Chaos] Request #{count}: injecting {delayMs}ms delay");
            await Task.Delay(delayMs);
        }

        // Every 10th request: return an Internal error (gRPC equivalent of HTTP 500)
        if (count % 10 == 0)
        {
            Console.WriteLine($"[Chaos] Request #{count}: returning Internal error");
            throw new RpcException(new Status(StatusCode.Internal, "Simulated internal server error"));
        }

        try
        {
            return new HelloReply { Message = "Hello " + request.Name };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled exception: {ex}");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}

