using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound($"User with ID {dto.UserId} not found.");

            if (!dto.IsFocusMode)
            {
                var team = _context.Teams.FirstOrDefault(t => t.name == dto.TeamName);
                if (team == null)
                    return BadRequest("Team not found.");

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

    }
}
