using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class DeskAssigner
    {
        private readonly AppDbContext _context;

        public DeskAssigner(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> AssignAsync(DateOnly date)
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date)
                .ToListAsync();

            var desks = await _context.Desks.Include(d => d.Zone).ToListAsync();

            // Lista dostêpnych biurek po Zone i Type
            var availableDesks = desks
                .GroupBy(d => new { d.Zone.Priority, d.DeskType })
                .OrderBy(g => g.Key.Priority)
                .ToDictionary(
                    g => g.Key,
                    g => new Queue<Desk>(g.ToList())
                );

            var failed = new List<string>();

            var groupedByTeam = reservations.GroupBy(r => r.User.TeamId);

            foreach (var teamGroup in groupedByTeam)
            {
                var deskType = teamGroup.First().DeskTypePref;
                var teamName = teamGroup.First().User.Team.name;
                var assigned = false;

                foreach (var kvp in availableDesks)
                {
                    var key = kvp.Key;
                    if (key.DeskType != deskType || kvp.Value.Count < teamGroup.Count())
                        continue;

                    foreach (var res in teamGroup)
                    {
                        var desk = kvp.Value.Dequeue();
                        res.AssignedDeskId = desk.Id;
                    }

                    assigned = true;
                    break;
                }

                if (!assigned)
                    failed.Add(teamName);
            }

            await _context.SaveChangesAsync();
            return failed;
        }
    }
}
