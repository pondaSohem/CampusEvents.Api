using System.ComponentModel.DataAnnotations;

namespace CampusEvents.Api.DTOs.Categories;

public class UpdateCategoryDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}