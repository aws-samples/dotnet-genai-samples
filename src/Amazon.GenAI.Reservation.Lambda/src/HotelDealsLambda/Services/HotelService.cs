using HotelDealsLambda.Models;

namespace HotelDealsLambda.Services;

public interface IHotelService
{
    Task<string> GetSpecialDealsAsync();
    Task<List<Room>> GetAvailableRoomsAsync(DateTime date);
    Task<Booking> BookRoomAsync(string roomNumber, DateTime date, decimal price);
}

public class HotelService : IHotelService
{
    private readonly List<Room> _rooms = new()
    {
        new Room { RoomNumber = "101", RoomType = "Standard", Price = 100, Description = "Cozy standard room with a queen-sized bed." },
        new Room { RoomNumber = "102", RoomType = "Standard", Price = 100, Description = "Spacious standard room with two double beds." },
        new Room { RoomNumber = "103", RoomType = "Deluxe", Price = 150, Description = "Luxurious deluxe room with a king-sized bed and a view." },
        new Room { RoomNumber = "104", RoomType = "Suite", Price = 200, Description = "Elegant suite with a separate living area and bedroom." },
        new Room { RoomNumber = "105", RoomType = "Standard", Price = 90, Description = "Comfortable standard room with modern amenities." },
        new Room { RoomNumber = "106", RoomType = "Deluxe", Price = 155, Description = "Deluxe room with premium furnishings and city view." },
        new Room { RoomNumber = "107", RoomType = "Standard", Price = 95, Description = "Well-appointed standard room with garden view." },
        new Room { RoomNumber = "108", RoomType = "Suite", Price = 220, Description = "Luxury suite with jacuzzi and panoramic view." },
        new Room { RoomNumber = "109", RoomType = "Deluxe", Price = 160, Description = "Deluxe room with luxurious bathroom and balcony." },
        new Room { RoomNumber = "110", RoomType = "Standard", Price = 85, Description = "Budget-friendly standard room with essential amenities." }
    };

    public Task<string> GetSpecialDealsAsync()
    {
        return Task.FromResult("The following hotel special deals are currently available: — Monday Staycation Special: 20% off room rates (Mondays only) — Last Minute Getaway: 15% off same-day bookings (Tuesdays only) — Extended Stay Discount: 20% off 3-night stays (Wednesdays only) — Suite Upgrade: Complimentary upgrade to executive suite (Thursdays only) — Weekend Getaway Package: 10% off 2-night stays (Fridays only)");
    }

    public Task<List<Room>> GetAvailableRoomsAsync(DateTime date)
    {
        return Task.FromResult(_rooms.Where(r => r.IsAvailable).ToList());
    }

    public Task<Booking> BookRoomAsync(string roomNumber, DateTime date, decimal price)
    {
        var room = _rooms.FirstOrDefault(r => r.RoomNumber == roomNumber);
        if (room == null)
            throw new ArgumentException($"Room {roomNumber} not found");

        var booking = new Booking
        {
            BookingId = Guid.NewGuid().ToString(),
            RoomNumber = roomNumber,
            RoomType = room.RoomType,
            Price = price,
            BookingDate = date,
            Description = room.Description
        };

        room.IsAvailable = false;
        return Task.FromResult(booking);
    }
}