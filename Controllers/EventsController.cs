using CampusEvents.Api.Data;
using CampusEvents.Api.DTOs.Events;
using CampusEvents.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        var events = await _context.Events
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Price = e.Price,
                Capacity = e.Capacity,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,

                AvailableTickets =
                    e.Capacity -
                    e.Bookings
                        .Where(b => b.Status != BookingStatus.Cancelled)
                        .Sum(b => b.NumberOfTickets)
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET: api/events/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        var eventDto = await _context.Events
            .Where(e => e.Id == id)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Price = e.Price,
                Capacity = e.Capacity,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,

                AvailableTickets =
                    e.Capacity -
                    e.Bookings
                        .Where(b => b.Status != BookingStatus.Cancelled)
                        .Sum(b => b.NumberOfTickets)
            })
            .FirstOrDefaultAsync();

        if (eventDto == null)
            return NotFound();

        return Ok(eventDto);
    }

    // POST: api/events
    // Admin only
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent(
        CreateEventDto dto)
    {
        // Validate dates.
        if (dto.EndDate <= dto.StartDate)
        {
            return BadRequest(
                "End date must be after the start date.");
        }

        // Validate category.
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(
                "Category does not exist.");
        }

        var newEvent = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Location = dto.Location,
            Price = dto.Price,
            Capacity = dto.Capacity,
            CategoryId = dto.CategoryId
        };

        _context.Events.Add(newEvent);

        await _context.SaveChangesAsync();

        var result = await _context.Events
            .Where(e => e.Id == newEvent.Id)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Price = e.Price,
                Capacity = e.Capacity,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                AvailableTickets = e.Capacity
            })
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = newEvent.Id },
            result);
    }

    // PUT: api/events/5
    // Admin only
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        UpdateEventDto dto)
    {
        // Validate dates.
        if (dto.EndDate <= dto.StartDate)
        {
            return BadRequest(
                "End date must be after the start date.");
        }

        var existingEvent = await _context.Events
            .FindAsync(id);

        if (existingEvent == null)
            return NotFound();

        // Validate category.
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(
                "Category does not exist.");
        }

        // Calculate currently booked tickets.
        var bookedTickets = await _context.Bookings
            .Where(b =>
                b.EventId == id &&
                b.Status != BookingStatus.Cancelled)
            .SumAsync(b => b.NumberOfTickets);

        // Don't allow capacity to be lower than
        // the number of tickets already booked.
        if (dto.Capacity < bookedTickets)
        {
            return BadRequest(
                $"Capacity cannot be less than the number of booked tickets ({bookedTickets}).");
        }

        existingEvent.Title = dto.Title;
        existingEvent.Description = dto.Description;
        existingEvent.ImageUrl = dto.ImageUrl;
        existingEvent.StartDate = dto.StartDate;
        existingEvent.EndDate = dto.EndDate;
        existingEvent.Location = dto.Location;
        existingEvent.Price = dto.Price;
        existingEvent.Capacity = dto.Capacity;
        existingEvent.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/events/5
    // Admin only
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var existingEvent = await _context.Events
            .Include(e => e.Bookings)
            .Include(e => e.Reviews)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (existingEvent == null)
            return NotFound();

        // Don't delete events that have booking history.
        if (existingEvent.Bookings.Any())
        {
            return BadRequest(
                "Cannot delete an event that has bookings.");
        }

        // Don't delete events that have reviews.
        if (existingEvent.Reviews.Any())
        {
            return BadRequest(
                "Cannot delete an event that has reviews.");
        }

        _context.Events.Remove(existingEvent);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}