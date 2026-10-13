using System.ComponentModel.DataAnnotations;

namespace CampusEvents.Api.DTOs.Events;

public class UpdateEventDto
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(1, 10000)]
    public int Capacity { get; set; }

    [Required]
    public int CategoryId { get; set; }
}