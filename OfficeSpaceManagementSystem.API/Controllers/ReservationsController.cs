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
                .Include(r => r.assignedDesk)
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Date,
                    r.CreatedAt,
                    r.DeskTypePref,
                    r.isFocusMode,
                    AssignedDeskName = r.assignedDesk != null ? r.assignedDesk.Name : null,
                    AssignedDeskType = r.assignedDesk != null ? r.assignedDesk.DeskType : (DeskType?)null
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
            var focusZoneTypes = new[] { ZoneType.Focus, ZoneType.DuoFocus, ZoneType.WarRoom };
            var focusDesks = allDesks.Where(d => focusZoneTypes.Contains(d.Zone.Type)).ToList();

            var hrZone = _context.Zones.FirstOrDefault(z => z.Name == "0-8");
            var hrZoneId = hrZone?.Id ?? -1;

            // 🔹 Ogólne biurka – tylko spoza 0-8
            var generalDesks = allDesks.Where(d => d.ZoneId != hrZoneId).ToList();

            var reservations = _context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == targetDate)
                .ToList();

            var reservedDeskIds = reservations
                .Where(r => r.AssignedDeskId != null)
                .Select(r => r.AssignedDeskId.Value)
                .ToHashSet();

            // 🔹 Liczenie total i free
            int allTotal = generalDesks.Count; // 223 - 4 = 219
            int allFree = generalDesks.Count(d => !reservedDeskIds.Contains(d.Id));

            // 🔹 Focus desks
            int focusTotal = focusDesks.Count;
            int focusReserved = reservations.Count(r =>
                r.AssignedDeskId != null &&
                focusZoneTypes.Contains(r.assignedDesk.Zone.Type)
            );
            int focusFree = focusDesks.Count(d => !reservedDeskIds.Contains(d.Id));

            var hrUserIds = reservations
                .Where(r => r.User != null && r.User.Team != null && r.User.Team.name.Trim().Equals("HR", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.UserId)
                .Distinct()
                .ToList();

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