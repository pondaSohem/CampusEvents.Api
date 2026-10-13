namespace CampusEvents.Api.Models;

public class Review
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}