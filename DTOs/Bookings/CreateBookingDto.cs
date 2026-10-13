using System.ComponentModel.DataAnnotations;

namespace CampusEvents.Api.DTOs.Bookings;

public class CreateBookingDto
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, 20)]
    public int NumberOfTickets { get; set; }
}