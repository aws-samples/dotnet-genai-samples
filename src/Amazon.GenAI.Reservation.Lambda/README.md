# Hotel Deals Lambda Function

This Lambda function provides hotel reservation services including special deals, room availability, and booking functionality.

## Features

- **Get Special Deals**: Returns current hotel special deals and promotions
- **Check Room Availability**: Lists available rooms for a specific date
- **Book Hotel Room**: Books a room for a specific date and price

## API Functions

### GetHotelSpecialDeals
Returns current special deals available for hotels.

**Response Example:**
```
The following hotel special deals are currently available: 
— Monday Staycation Special: 20% off room rates (Mondays only) 
— Last Minute Getaway: 15% off same-day bookings (Tuesdays only) 
— Extended Stay Discount: 20% off 3-night stays (Wednesdays only) 
— Suite Upgrade: Complimentary upgrade to executive suite (Thursdays only) 
— Weekend Getaway Package: 10% off 2-night stays (Fridays only)
```

### GetAvailableRooms
Gets available hotel rooms for a specific date.

**Parameters:**
- `date` (string): Date in YYYY-MM-DD format

**Response Example:**
```
Here are the available rooms on 2024-02-25: 
— Room 101 (Standard, $100): Cozy standard room with a queen-sized bed. 
— Room 102 (Standard, $100): Spacious standard room with two double beds. 
— Room 103 (Deluxe, $150): Luxurious deluxe room with a king-sized bed and a view.
```

### BookHotelRoom
Books a hotel room for a specific date and price.

**Parameters:**
- `roomNumber` (string): Room number to book
- `date` (string): Date in YYYY-MM-DD format
- `price` (decimal): Booking price

**Response Example:**
```
I have booked room 109 for you on 2024-02-25. This is a deluxe room with luxurious bathroom and balcony for $160. Please let me know if you need anything else!
```

## Deployment

```bash
sam build
sam deploy
```

## Usage with Bedrock Agent

This function is designed to work with Amazon Bedrock Agents. Configure your Bedrock Agent to use this Lambda function as an action group to enable hotel reservation capabilities.