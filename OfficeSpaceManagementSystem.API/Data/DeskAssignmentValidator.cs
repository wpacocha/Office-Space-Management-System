using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class DeskAssignmentValidator
    {
        private readonly AppDbContext _context;

        public DeskAssignmentValidator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, List<string> FailedTeams)> ValidateAsync(DateOnly date)
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date)
                .ToListAsync();

            var zones = await _context.Zones.OrderBy(z => z.Type).ToListAsync();

            // dostêpne biurka per typ per strefa
            var zoneDesksAvailable = zones.ToDictionary(
                z => z.Id,
                z => new Dictionary<DeskType, int>
                {
                    { DeskType.WideMonitor, z.WideMonitorDesks },
                    { DeskType.DualMonitor, z.DualMonitorDesks }
                });

            var groupedByTeam = reservations.GroupBy(r => r.User.TeamId);
            List<string> failedTeams = new();

            foreach (var teamGroup in groupedByTeam)
            {
                var teamName = teamGroup.First().User.Team.name;
                var deskType = teamGroup.First().DeskTypePref;
                int teamSize = teamGroup.Count();

                bool assigned = false;

                foreach (var zone in zones)
                {
                    if (zoneDesksAvailable[zone.Id][deskType] >= teamSize)
                    {
                        zoneDesksAvailable[zone.Id][deskType] -= teamSize;
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    failedTeams.Add(teamName);
                }
            }

            return (failedTeams.Count == 0, failedTeams);
        }
    }
}
