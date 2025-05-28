using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using OfficeSpaceManagementSystem.API.Models;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DeskAssigner _deskAssigner;

    public AdminController(AppDbContext context)
    {
        _context = context;
        _deskAssigner = new DeskAssigner(_context);
    }

    [HttpPost("seed")]
    public IActionResult SeedAll([FromQuery] DateOnly? date = null, [FromQuery] int count = 200)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var options = new SeedOptions
        {
            ReservationDate = targetDate,
            ReservationsCount = count
        };

        DbSeeder.Seed(_context, options);

        return Ok(new
        {
            message = $"Seeded database with {count} reservations for {targetDate}."
        });
    }




    [HttpGet("focus-availability")]
    public IActionResult GetFocusAvailability()
    {
        // policz dost�pne biurka w strefach typu Focus
        var focusZones = _context.Zones
            .Where(z => z.Type == ZoneType.Focus)
            .ToList();

        var focusDesks = _context.Desks
            .Where(d => focusZones.Select(z => z.Id).Contains(d.ZoneId))
            .ToList();

        var reservedDeskIds = _context.Reservations
            .Where(r => r.Date == DateOnly.FromDateTime(DateTime.Today) && r.AssignedDeskId != null)
            .Select(r => r.AssignedDeskId.Value)
            .ToHashSet();

        int available = focusDesks.Count(d => !reservedDeskIds.Contains(d.Id));

        return Ok(new { available });
    }



    [HttpPost("assign")]
    public async Task<IActionResult> RunAssignment([FromQuery] DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var failures = await _deskAssigner.AssignAsync(targetDate);

        var assigned = _context.Reservations
            .Include(r => r.User)
            .ThenInclude(u => u.Team)
            .Include(r => r.assignedDesk)
            .ThenInclude(d => d.Zone)
            .Where(r => r.Date == targetDate && r.AssignedDeskId != null)
            .Select(r => new
            {
                name = r.User.Name,
                email = r.User.Email,
                team = r.User.Team.name,
                date = r.Date,
                deskName = r.assignedDesk.Name,
                zone = r.assignedDesk.Zone.Name
            })
            .ToList();


        return Ok(new
        {
            message = $"Assignment completed for {targetDate}",
            failed = failures,
            assigned
        });
    }

    [HttpPost("generate-reservations")]
    public IActionResult GenerateReservations([FromQuery] DateOnly? date = null, [FromQuery] int count = 200)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        ReservationGenerator.Generate(_context, targetDate, count);

        return Ok(new
        {
            message = $"Generated {count} reservations for {targetDate}."
        });
    }

}
