namespace CampusEvents.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public int NumberOfTickets { get; set; }

    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public Event Event { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled
}