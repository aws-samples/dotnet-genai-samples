using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GrpcHelloService.Interceptors;

public class ExceptionInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            // Let RpcExceptions pass through — they already have proper status codes
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled exception in {context.Method}: {ex}");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
