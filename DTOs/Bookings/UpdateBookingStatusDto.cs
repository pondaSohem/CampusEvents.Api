using System.ComponentModel.DataAnnotations;
using CampusEvents.Api.Models;

namespace CampusEvents.Api.DTOs.Bookings;

public class UpdateBookingStatusDto
{
    [Required]
    public BookingStatus Status { get; set; }
}