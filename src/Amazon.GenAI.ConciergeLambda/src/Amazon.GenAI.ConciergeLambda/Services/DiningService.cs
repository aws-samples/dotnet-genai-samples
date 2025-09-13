using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.GenAI.ConciergeLambda.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;

namespace Amazon.GenAI.ConciergeLambda.Services;

public interface IDiningService
{
    Task<Guid> CreateDiningReservationAsync(string guestName, DateTime reservationDateTime, int numberOfGuests, MealType? mealType = null);
}

public class DiningService : IDiningService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _tableName;
    private readonly string _snsTopicArn;

    public DiningService(IAmazonDynamoDB dynamoDbClient, IAmazonSimpleNotificationService snsClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _snsClient = snsClient;
        _tableName = Environment.GetEnvironmentVariable("DINING_RESERVATION") ?? "DiningReservations";
        _snsTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") ?? throw new InvalidOperationException("SNS_TOPIC_ARN environment variable is required");
    }

    public async Task<Guid> CreateDiningReservationAsync(string guestName, DateTime reservationDateTime, int numberOfGuests, MealType? mealType = null)
    {
        // Validate future date/time
        if (reservationDateTime <= DateTime.UtcNow)
        {
            throw new ArgumentException("Reservation must be in the future");
        }

        // Validate number of guests
        if (numberOfGuests < 1 || numberOfGuests > 12)
        {
            throw new ArgumentException("Number of guests must be between 1 and 12");
        }

        var hour = reservationDateTime.Hour;

        // Validate operating hours and infer meal type
        MealType inferredMealType;
        if (hour >= 12 && hour < 16)
        {
            inferredMealType = MealType.Lunch;
        }
        else if (hour >= 18 && hour < 22)
        {
            inferredMealType = MealType.Dinner;
        }
        else
        {
            throw new ArgumentException("Reservation time must be during lunch (12:00-16:00) or dinner (18:00-22:00) hours");
        }

        var finalMealType = mealType ?? inferredMealType;
        var reservationId = Guid.NewGuid();

        var reservation = new DiningReservation
        {
            ReservationId = reservationId,
            GuestName = guestName,
            ReservationDateTime = reservationDateTime.ToString("yyyy/MM/dd HH:mm:ss"),
            NumberOfGuests = numberOfGuests,
            MealType = finalMealType,
            RestaurantName = "Octank Dine",
            Status = ReservationStatus.Confirmed
        };

        // Insert into DynamoDB
        var putRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["ReservationId"] = new AttributeValue { S = reservation.ReservationId.ToString() },
                ["GuestName"] = new AttributeValue { S = reservation.GuestName },
                ["ReservationDateTime"] = new AttributeValue { S = reservation.ReservationDateTime },
                ["NumberOfGuests"] = new AttributeValue { N = reservation.NumberOfGuests.ToString() },
                ["MealType"] = new AttributeValue { S = reservation.MealType.ToString() },
                ["RestaurantName"] = new AttributeValue { S = reservation.RestaurantName },
                ["Status"] = new AttributeValue { S = reservation.Status.ToString() }
            }
        };

        await _dynamoDbClient.PutItemAsync(putRequest);

        // Send SNS notification
        var snsRequest = new PublishRequest
        {
            TopicArn = _snsTopicArn,
            Message = JsonSerializer.Serialize(reservation)
        };

        await _snsClient.PublishAsync(snsRequest);

        return reservationId;
    }
}
