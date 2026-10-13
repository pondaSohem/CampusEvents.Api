namespace CampusEvents.Api.DTOs.Reviews;

public class ReviewDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}