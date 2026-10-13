using System.Security.Claims;
using CampusEvents.Api.Data;
using CampusEvents.Api.DTOs.Reviews;
using CampusEvents.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/reviews/event/5
    [HttpGet("event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetEventReviews(
        int eventId)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == eventId);

        if (!eventExists)
            return NotFound("Event not found.");

        var reviews = await _context.Reviews
            .Where(r => r.EventId == eventId)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                EventId = r.EventId,
                EventTitle = r.Event.Title,
                UserName = r.User.FirstName + " " + r.User.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // POST: api/reviews
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview(
        CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == dto.EventId);

        if (!eventExists)
            return NotFound("Event not found.");

        // Check whether the user has booked this event.
        var hasBooking = await _context.Bookings
            .AnyAsync(b =>
                b.UserId == userId &&
                b.EventId == dto.EventId &&
                b.Status != BookingStatus.Cancelled);

        if (!hasBooking)
        {
            return BadRequest(
                "You must have a valid booking for this event before leaving a review.");
        }

        // Prevent multiple reviews from the same user
        // for the same event.
        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r =>
                r.UserId == userId &&
                r.EventId == dto.EventId);

        if (alreadyReviewed)
        {
            return BadRequest(
                "You have already reviewed this event.");
        }

        var review = new Review
        {
            EventId = dto.EventId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        var result = await _context.Reviews
            .Where(r => r.Id == review.Id)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                EventId = r.EventId,
                EventTitle = r.Event.Title,
                UserName = r.User.FirstName + " " + r.User.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetEventReviews),
            new { eventId = review.EventId },
            result);
    }

    // DELETE: api/reviews/5
    // Owner OR Admin
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var review = await _context.Reviews
            .FindAsync(id);

        if (review == null)
            return NotFound();

        if (review.UserId != userId &&
            !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        _context.Reviews.Remove(review);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}