using HotelDealsLambda.Models;

namespace HotelDealsLambda.Services;

public interface IHotelService
{
    Task<string> GetSpecialDealsAsync();
    Task<List<Room>> GetAvailableRoomsAsync(DateTime date, int guests, string location);
    Task<Booking> BookRoomAsync(string roomType, DateTime checkIn, DateTime checkOut, int guests, string guestName, string location);
}

/// <summary>
/// Hotel Service Implementation
/// 
/// IMPORTANT: This is a demonstration implementation using in-memory data.
/// 
/// For production use, replace this with integration to:
/// - Hotel Property Management Systems (PMS) 
/// - Real-time inventory management systems
/// - Rate and availability APIs from hotel chains
/// - Payment processing systems (Stripe, Square, etc.)
/// - Database systems (DynamoDB, RDS) for persistent storage
/// 
/// Example integrations:
/// - AWS RDS for reservation data
/// - DynamoDB for room inventory and pricing
/// - External APIs for real-time availability
/// - SQS/SNS for booking confirmations and notifications
/// </summary>
public class HotelService : IHotelService
{
    // NOTE: In-memory room data for demonstration purposes only.
    // In production, this data would be retrieved from:
    // - Hotel reservation systems (Opera PMS, Amadeus, Sabre)
    // - Real-time inventory APIs
    // - Database systems (DynamoDB, RDS)
    // - Channel management systems
    private readonly List<Room> _rooms = new()
    {
        // Chicago Location
        new Room { RoomType = "Standard King", Price = 129, MaxGuests = 2, Description = "Comfortable room with one king bed", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning"} },
        new Room { RoomType = "Standard Double", Price = 139, MaxGuests = 4, Description = "Spacious room with two double beds", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge"} },
        new Room { RoomType = "Deluxe King", Price = 179, MaxGuests = 2, Description = "Premium room with king bed and city view", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk"} },
        new Room { RoomType = "Junior Suite", Price = 249, MaxGuests = 4, Description = "Suite with separate seating area and king bed", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk", "Sofa Bed"} },
        new Room { RoomType = "Executive Suite", Price = 349, MaxGuests = 6, Description = "Luxury suite with separate bedroom and living room", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Sofa Bed", "Balcony"} },
        new Room { RoomType = "Family Room", Price = 199, MaxGuests = 6, Description = "Large room with bunk beds and queen bed", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Microwave"} },
        new Room { RoomType = "Accessible King", Price = 129, MaxGuests = 2, Description = "ADA compliant room with king bed", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Roll-in Shower"} },
        new Room { RoomType = "Presidential Suite", Price = 599, MaxGuests = 8, Description = "Luxury presidential suite with multiple bedrooms", Location = "Chicago", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Dining Area", "Balcony", "Jacuzzi"} },
        
        // San Francisco Location
        new Room { RoomType = "Standard King", Price = 159, MaxGuests = 2, Description = "Comfortable room with one king bed", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning"} },
        new Room { RoomType = "Standard Double", Price = 169, MaxGuests = 4, Description = "Spacious room with two double beds", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge"} },
        new Room { RoomType = "Deluxe King", Price = 209, MaxGuests = 2, Description = "Premium room with king bed and bay view", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk"} },
        new Room { RoomType = "Junior Suite", Price = 279, MaxGuests = 4, Description = "Suite with separate seating area and king bed", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk", "Sofa Bed"} },
        new Room { RoomType = "Executive Suite", Price = 399, MaxGuests = 6, Description = "Luxury suite with separate bedroom and living room", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Sofa Bed", "Balcony"} },
        new Room { RoomType = "Family Room", Price = 229, MaxGuests = 6, Description = "Large room with bunk beds and queen bed", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Microwave"} },
        new Room { RoomType = "Accessible King", Price = 159, MaxGuests = 2, Description = "ADA compliant room with king bed", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Roll-in Shower"} },
        new Room { RoomType = "Presidential Suite", Price = 699, MaxGuests = 8, Description = "Luxury presidential suite with multiple bedrooms", Location = "San Francisco", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Dining Area", "Balcony", "Jacuzzi"} },
        
        // London Location
        new Room { RoomType = "Standard King", Price = 149, MaxGuests = 2, Description = "Comfortable room with one king bed", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning"} },
        new Room { RoomType = "Standard Double", Price = 159, MaxGuests = 4, Description = "Spacious room with two double beds", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge"} },
        new Room { RoomType = "Deluxe King", Price = 199, MaxGuests = 2, Description = "Premium room with king bed and Thames view", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk"} },
        new Room { RoomType = "Junior Suite", Price = 269, MaxGuests = 4, Description = "Suite with separate seating area and king bed", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Work Desk", "Sofa Bed"} },
        new Room { RoomType = "Executive Suite", Price = 379, MaxGuests = 6, Description = "Luxury suite with separate bedroom and living room", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Sofa Bed", "Balcony"} },
        new Room { RoomType = "Family Room", Price = 219, MaxGuests = 6, Description = "Large room with bunk beds and queen bed", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Mini Fridge", "Microwave"} },
        new Room { RoomType = "Accessible King", Price = 149, MaxGuests = 2, Description = "ADA compliant room with king bed", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Roll-in Shower"} },
        new Room { RoomType = "Presidential Suite", Price = 649, MaxGuests = 8, Description = "Luxury presidential suite with multiple bedrooms", Location = "London", Amenities = new List<string> {"Free WiFi", "Coffee Maker", "Air Conditioning", "Full Kitchen", "Work Desk", "Dining Area", "Balcony", "Jacuzzi"} }
    };

    public Task<string> GetSpecialDealsAsync()
    {
        return Task.FromResult("The following hotel special deals are currently available: — Monday Staycation Special: 20% off room rates (Mondays only) — Last Minute Getaway: 15% off same-day bookings (Tuesdays only) — Extended Stay Discount: 20% off 3-night stays (Wednesdays only) — Suite Upgrade: Complimentary upgrade to executive suite (Thursdays only) — Weekend Getaway Package: 10% off 2-night stays (Fridays only)");
    }

    public Task<List<Room>> GetAvailableRoomsAsync(DateTime date, int guests, string location)
    {
        // NOTE: In production, this method would:
        // - Query real-time availability from PMS systems
        // - Check room inventory in database (DynamoDB/RDS)
        // - Apply dynamic pricing based on demand
        // - Consider existing reservations and blocked dates
        // - Integrate with channel managers for multi-property availability
        return Task.FromResult(_rooms.Where(r => r.IsAvailable && r.MaxGuests >= guests && 
            string.Equals(r.Location, location, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<Booking> BookRoomAsync(string roomType, DateTime checkIn, DateTime checkOut, int guests, string guestName, string location)
    {
        var room = _rooms.FirstOrDefault(r => r.RoomType == roomType && r.IsAvailable && r.MaxGuests >= guests &&
            string.Equals(r.Location, location, StringComparison.OrdinalIgnoreCase));
        if (room == null)
            throw new ArgumentException($"{roomType} room not available for {guests} guests in {location}");

        var nights = (checkOut - checkIn).Days;
        var totalPrice = room.Price * nights;
        
        var booking = new Booking
        {
            BookingId = Guid.NewGuid().ToString(),
            ConfirmationNumber = $"HTL{Random.Shared.Next(100000, 999999)}",
            RoomType = room.RoomType,
            PricePerNight = room.Price,
            TotalPrice = totalPrice,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Guests = guests,
            GuestName = guestName,
            Description = room.Description,
            Location = room.Location
        };

        room.IsAvailable = false;
        return Task.FromResult(booking);
    }
}