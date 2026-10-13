namespace CampusEvents.Api.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public int NumberOfTickets { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}