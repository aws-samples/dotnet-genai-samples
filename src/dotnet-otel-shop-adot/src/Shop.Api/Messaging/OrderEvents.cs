using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Shop.Api.Data;
using Shop.Api.Diagnostics;
using Shop.Contracts;

namespace Shop.Api.Messaging;

public record OrderPlacedEvent(string OrderId, string SessionId, decimal Total, int ItemCount, DateTimeOffset PlacedAt);

public interface IOrderEventPublisher
{
    Task PublishAsync(OrderPlacedEvent evt, CancellationToken ct);
}

/// <summary>
/// Publishes to SQS. The AWS SDK instrumentation injects the W3C traceparent
/// into the message attributes, so the consumer can continue the same trace.
/// </summary>
public class SqsOrderEventPublisher(IAmazonSQS sqs, IOptions<ShopOptions> options) : IOrderEventPublisher
{
    public Task PublishAsync(OrderPlacedEvent evt, CancellationToken ct) =>
        sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = options.Value.Aws.OrderQueueUrl,
            MessageBody = JsonSerializer.Serialize(evt),
            MessageAttributes = new()
            {
                ["event-type"] = new MessageAttributeValue { DataType = "String", StringValue = "OrderPlaced" },
            },
        }, ct);
}

/// <summary>
/// Local stand-in for SQS. Propagates trace context by hand, which is exactly
/// what the AWS instrumentation does for you with SQS message attributes.
/// </summary>
public class InMemoryOrderQueue : IOrderEventPublisher
{
    private readonly Channel<(OrderPlacedEvent Event, Dictionary<string, string> Headers)> _channel =
        Channel.CreateUnbounded<(OrderPlacedEvent, Dictionary<string, string>)>();

    public ChannelReader<(OrderPlacedEvent Event, Dictionary<string, string> Headers)> Reader => _channel.Reader;

    public async Task PublishAsync(OrderPlacedEvent evt, CancellationToken ct)
    {
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("order-events publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "in-memory");
        activity?.SetTag("messaging.destination.name", "order-events");

        var headers = new Dictionary<string, string>();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
            headers,
            (carrier, key, value) => carrier[key] = value);

        await _channel.Writer.WriteAsync((evt, headers), ct);
    }
}

/// <summary>Shared fulfillment logic for both the SQS and in-memory consumers.</summary>
public class OrderFulfillmentProcessor(IOrderRepository orders, ILogger<OrderFulfillmentProcessor> logger, ILoggerFactory loggers)
{
    // Plain-English demo log, same "ShopperJourney" category the storefront uses.
    private readonly ILogger _journey = loggers.CreateLogger("ShopperJourney");

    public async Task ProcessAsync(OrderPlacedEvent evt, PropagationContext parent, string system, CancellationToken ct)
    {
        Baggage.Current = parent.Baggage;
        using var activity = ShopDiagnostics.ActivitySource.StartActivity(
            "order-events process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", system);
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("order.id", evt.OrderId);
        ShopLog.OrderEventReceived(logger, evt.OrderId, system, (long)(DateTimeOffset.UtcNow - evt.PlacedAt).TotalMilliseconds);

        // Pretend to pick, pack, and ship.
        await Task.Delay(Random.Shared.Next(100, 400), ct);
        await orders.UpdateStatusAsync(evt.OrderId, OrderStatus.Fulfilled, ct);

        ShopDiagnostics.OrdersFulfilled.Add(1);
        ShopLog.OrderFulfilled(logger, evt.OrderId, evt.ItemCount, evt.Total, (long)(DateTimeOffset.UtcNow - evt.PlacedAt).TotalMilliseconds);
        ShopLog.OrderShipped(_journey, evt.OrderId, evt.SessionId[..Math.Min(6, evt.SessionId.Length)], evt.ItemCount, evt.Total);
    }
}

public class InMemoryFulfillmentWorker(InMemoryOrderQueue queue, OrderFulfillmentProcessor processor, ILogger<InMemoryFulfillmentWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (evt, headers) in queue.Reader.ReadAllAsync(ct))
        {
            var parent = Propagators.DefaultTextMapPropagator.Extract(default, headers,
                (carrier, key) => carrier.TryGetValue(key, out var v) ? [v] : []);
            try
            {
                await processor.ProcessAsync(evt, parent, "in-memory", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ShopLog.FulfillmentFailed(logger, ex, evt.OrderId);
            }
        }
    }
}

public class SqsFulfillmentWorker(
    IAmazonSQS sqs,
    IOptions<ShopOptions> options,
    OrderFulfillmentProcessor processor,
    ILogger<SqsFulfillmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var queueUrl = options.Value.Aws.OrderQueueUrl;
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning("Shop:Aws:OrderQueueUrl is not set; order fulfillment worker is disabled");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = ["All"],
                }, ct);

                foreach (var message in response.Messages ?? [])
                {
                    var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(message.Body)!;
                    var attributes = message.MessageAttributes ?? [];
                    var parent = Propagators.DefaultTextMapPropagator.Extract(default, attributes,
                        (carrier, key) => carrier.TryGetValue(key, out var v) && v.StringValue is { } s ? [s] : []);

                    await processor.ProcessAsync(evt, parent, "aws_sqs", ct);
                    await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Failed messages become visible again and land in the DLQ after retries.
                logger.LogError(ex, "Error processing order events");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
}
