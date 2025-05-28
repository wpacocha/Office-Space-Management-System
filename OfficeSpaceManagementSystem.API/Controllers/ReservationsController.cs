using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using OfficeSpaceManagementSystem.API.DTOs;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var requestedDate = dto.Date;

            if (requestedDate == today)
            {
                return BadRequest("You cannot make a reservation for today.");
            }

            if (requestedDate == today.AddDays(1) && now.Hour >= 15)
            {
                return BadRequest("You can no longer make a reservation for tomorrow after 15:00.");
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound($"User with ID {dto.UserId} not found.");

            var existing = _context.Reservations.FirstOrDefault(r => r.UserId == dto.UserId && r.Date == dto.Date);
            if (existing != null)
                return BadRequest("You already have a reservation for this day.");


            if (!dto.IsFocusMode)
            {
                var team = _context.Teams.FirstOrDefault(t => t.name == dto.TeamName);
                if (team == null)
                {
                    team = new Team { name = dto.TeamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }
                user.TeamId = team.Id;


                user.TeamId = team.Id;
            }

            var reservation = new Reservation
            {
                UserId = dto.UserId,
                Date = dto.Date,
                CreatedAt = DateTime.Now,
                DeskTypePref = dto.DeskTypePref,
                isFocusMode = dto.IsFocusMode,
                AssignedDeskId = null
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok("Reservation created successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserReservations([FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with ID {userId} not found.");

            var reservations = _context.Reservations
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Date,
                    r.CreatedAt,
                    r.DeskTypePref,
                    r.isFocusMode,
                    AssignedDeskName = r.assignedDesk != null ? r.assignedDesk.Name : null
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            return Ok(reservations);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound($"Reservation with ID {id} not found.");

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return Ok("Reservation cancelled.");
        }

        [HttpGet("by-date")]
        public IActionResult GetReservationsByDate([FromQuery] DateOnly date)
        {
            var reservations = _context.Reservations
                .Where(r => r.Date == date)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    UserName = r.User.Name,
                    TeamName = r.User.Team.name,
                    r.isFocusMode,
                    r.DeskTypePref,
                    DeskName = r.assignedDesk != null ? r.assignedDesk.Name : null
                })
                .OrderBy(r => r.UserName)
                .ToList();

            return Ok(reservations);
        }
        [HttpGet("availability")]
        public IActionResult GetAvailability([FromQuery] DateOnly? date = null)
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            var allDesks = _context.Desks.Include(d => d.Zone).ToList();

            var reservations = _context.Reservations
                .Where(r => r.Date == targetDate)
                .ToList();

            var focusZoneIds = _context.Zones
                .Where(z =>
                    z.Type == ZoneType.Focus ||
                    z.Type == ZoneType.DuoFocus ||
                    z.Type == ZoneType.WarRoom)
                .Select(z => z.Id)
                .ToList();

            var focusDesks = allDesks
                .Where(d => focusZoneIds.Contains(d.ZoneId))
                .ToList();

            var allTotal = allDesks.Count;
            var allReservedCount = reservations.Count;
            var allFree = allTotal - allReservedCount;

            var focusTotal = focusDesks.Count;
            var focusReservedCount = reservations.Count(r => r.isFocusMode);
            var focusFree = focusTotal - focusReservedCount;

            return Ok(new
            {
                date = targetDate,
                all = new { free = allFree, total = allTotal },
                focus = new { free = focusFree, total = focusTotal },
                anyAvailable = allFree > 0
            });
        }

    }
}