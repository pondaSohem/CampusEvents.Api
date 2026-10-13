using System.Security.Claims;
using CampusEvents.Api.Data;
using CampusEvents.Api.DTOs.Bookings;
using CampusEvents.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // GET: api/bookings
    // ADMIN ONLY
    // Get all bookings
    // =========================================================

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookings()
    {
        var bookings = await _context.Bookings
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                UserId = b.UserId,
                BookingDate = b.BookingDate,
                NumberOfTickets = b.NumberOfTickets,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        return Ok(bookings);
    }


    // =========================================================
    // GET: api/bookings/my-bookings
    // LOGGED-IN USERS
    // Get bookings belonging to the current user
    // =========================================================

    [Authorize]
    [HttpGet("my-bookings")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var bookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                UserId = b.UserId,
                BookingDate = b.BookingDate,
                NumberOfTickets = b.NumberOfTickets,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        return Ok(bookings);
    }


    // =========================================================
    // GET: api/bookings/5
    // LOGGED-IN USERS
    // User can see their own booking.
    // ADMIN can see any booking.
    // =========================================================

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var booking = await _context.Bookings
            .Where(b => b.Id == id)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                UserId = b.UserId,
                BookingDate = b.BookingDate,
                NumberOfTickets = b.NumberOfTickets,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString()
            })
            .FirstOrDefaultAsync();

        if (booking == null)
            return NotFound();

        if (booking.UserId != userId &&
            !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        return Ok(booking);
    }


    // =========================================================
    // POST: api/bookings
    // LOGGED-IN USERS
    // Create a booking
    // =========================================================

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        CreateBookingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var eventItem = await _context.Events
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e => e.Id == dto.EventId);

        if (eventItem == null)
            return NotFound("Event not found.");

        if (dto.NumberOfTickets <= 0)
            return BadRequest(
                "Number of tickets must be greater than zero.");

        var bookedTickets = eventItem.Bookings
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Sum(b => b.NumberOfTickets);

        var availableTickets =
            eventItem.Capacity - bookedTickets;

        if (dto.NumberOfTickets > availableTickets)
        {
            return BadRequest(
                $"Only {availableTickets} tickets are available.");
        }

        var totalAmount =
            eventItem.Price * dto.NumberOfTickets;

        var booking = new Booking
        {
            EventId = dto.EventId,
            UserId = userId,
            BookingDate = DateTime.UtcNow,
            NumberOfTickets = dto.NumberOfTickets,
            TotalAmount = totalAmount,
            Status = BookingStatus.Pending
        };

        _context.Bookings.Add(booking);

        await _context.SaveChangesAsync();


        // Return the newly created booking as a DTO
        var result = await _context.Bookings
            .Where(b => b.Id == booking.Id)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                UserId = b.UserId,
                BookingDate = b.BookingDate,
                NumberOfTickets = b.NumberOfTickets,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString()
            })
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetBooking),
            new { id = booking.Id },
            result);
    }


    // =========================================================
    // PUT: api/bookings/5/cancel
    // LOGGED-IN USERS
    // User can cancel their own booking.
    // ADMIN can cancel any booking.
    // =========================================================

    [Authorize]
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var booking = await _context.Bookings
            .FindAsync(id);

        if (booking == null)
            return NotFound();

        if (booking.UserId != userId &&
            !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return BadRequest(
                "Booking is already cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // =========================================================
    // PATCH: api/bookings/5/status
    // ADMIN ONLY
    // Update booking status
    // =========================================================

    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateBookingStatus(
        int id,
        UpdateBookingStatusDto dto)
    {
        var booking = await _context.Bookings
            .FindAsync(id);

        if (booking == null)
            return NotFound();

        // Prevent changing a booking to an invalid status.
        if (!Enum.IsDefined(typeof(BookingStatus), dto.Status))
        {
            return BadRequest("Invalid booking status.");
        }

        booking.Status = dto.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}