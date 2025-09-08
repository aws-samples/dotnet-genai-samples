# Hotel Reservations Lambda Validation Prompts

## GetHotelSpecialDeals Function

### Valid Requests
- "What special deals do you have available?"
- "Show me current hotel promotions"
- "Are there any discounts available?"

### Expected Response
Should return formatted string with 5 special deals including Monday Staycation, Last Minute Getaway, Extended Stay Discount, Suite Upgrade, and Weekend Getaway Package.

## GetAvailableRooms Function

### Valid Requests
- "Show me available rooms for 2024-12-25 for 2 guests"
- "What rooms are available on 2024-01-15 for 4 people?"
- "Check availability for 2024-03-10 for 1 guest"

### Invalid Date Format Tests
- "Show rooms for Dec 25 2024 for 2 guests" (should return date format error)
- "Available rooms for 25/12/2024 for 2 guests" (should return date format error)

### Invalid Guest Count Tests
- "Rooms for 2024-12-25 for 0 guests" (should return guest count error)
- "Available rooms for 2024-12-25 for 10 guests" (should return guest count error)

### Expected Response
Should return formatted list of available rooms with type, price, max guests, and description.

## BookHotelRoom Function

### Valid Booking Tests
- "Book a Standard King room from 2024-12-25 to 2024-12-27 for 2 guests under John Smith"
- "Reserve Executive Suite from 2024-01-15 to 2024-01-18 for 4 guests under Jane Doe"

### Date Validation Tests
- Invalid check-in: "Book Standard King from Dec 25 to 2024-12-27 for 2 guests under John Smith"
- Invalid check-out: "Book Standard King from 2024-12-25 to Dec 27 for 2 guests under John Smith"
- Check-out before check-in: "Book Standard King from 2024-12-27 to 2024-12-25 for 2 guests under John Smith"
- Same dates: "Book Standard King from 2024-12-25 to 2024-12-25 for 2 guests under John Smith"

### Guest Count Validation Tests
- "Book Standard King from 2024-12-25 to 2024-12-27 for 0 guests under John Smith"
- "Book Standard King from 2024-12-25 to 2024-12-27 for 10 guests under John Smith"

### Guest Name Validation Tests
- "Book Standard King from 2024-12-25 to 2024-12-27 for 2 guests under "
- "Book Standard King from 2024-12-25 to 2024-12-27 for 2 guests"

### Room Availability Tests
- "Book Nonexistent Room from 2024-12-25 to 2024-12-27 for 2 guests under John Smith"
- "Book Standard King from 2024-12-25 to 2024-12-27 for 5 guests under John Smith" (exceeds max guests)

### Expected Response
Should return confirmation with room type, guest name, dates, nights, guest count, confirmation number, and total cost.

## Edge Cases

### Boundary Values
- Maximum guests (8): "Book Presidential Suite from 2024-12-25 to 2024-12-27 for 8 guests under John Smith"
- Minimum guests (1): "Book Standard King from 2024-12-25 to 2024-12-27 for 1 guest under John Smith"

### Long Stay Test
- "Book Standard King from 2024-12-25 to 2025-01-25 for 2 guests under John Smith" (31 nights)

### Special Characters in Name
- "Book Standard King from 2024-12-25 to 2024-12-27 for 2 guests under José María O'Connor-Smith"

## Room Types Available for Testing
- Standard King ($129, max 2 guests)
- Standard Double ($139, max 4 guests)
- Deluxe King ($179, max 2 guests)
- Junior Suite ($249, max 4 guests)
- Executive Suite ($349, max 6 guests)
- Family Room ($199, max 6 guests)
- Accessible King ($129, max 2 guests)
- Presidential Suite ($599, max 8 guests)