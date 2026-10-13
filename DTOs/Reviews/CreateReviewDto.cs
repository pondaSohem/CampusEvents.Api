using System.ComponentModel.DataAnnotations;

namespace CampusEvents.Api.DTOs.Reviews;

public class CreateReviewDto
{
    [Required]
    public int EventId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}