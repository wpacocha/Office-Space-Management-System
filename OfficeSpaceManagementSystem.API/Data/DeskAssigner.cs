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

            var desks = await _context.Desks
                .Include(d => d.Zone)
                .ToListAsync();

            var desksByZone = desks
                .GroupBy(d => d.ZoneId)
                .ToDictionary(g => g.Key, g => new Queue<Desk>(g.ToList()));

            var zonesByType = desks
                .Select(d => d.Zone)
                .Distinct()
                .GroupBy(z => z.Priority)
                .ToDictionary(g => g.Key, g => g.ToList());

            var failed = new List<string>();

            AssignPreferredZones(reservations, desksByZone, zonesByType);

            AssignFallbackZones(reservations, desksByZone, zonesByType, failed);

            await _context.SaveChangesAsync();
            return failed;
        }

        private static void AssignPreferredZones(List<Reservation> reservations, Dictionary<int, Queue<Desk>> desksByZone, Dictionary<int, List<Zone>> zonesByType)
        {
            var reservationsByPreference = reservations
                .GroupBy(r => r.ZonePreference)
                .OrderBy(g => g.Key);

            foreach (var preferenceGroup in reservationsByPreference)
            {
                var groupedByTeam = preferenceGroup
                    .GroupBy(r => r.User.Team)
                    .OrderByDescending(g => g.Count());

                foreach (var teamGroup in groupedByTeam)
                {
                    var teamReservations = teamGroup.ToList();
                    int teamSize = teamReservations.Count;

                    var preferredZones = zonesByType.GetValueOrDefault(preferenceGroup.Key, new List<Zone>())
                        .OrderByDescending(z => z.TotalDesks)
                        .ToList();

                    if (TryAssignWholeTeam(teamReservations, teamSize, preferredZones, desksByZone))
                        continue;

                    AssignTeamIndividually(teamReservations, preferredZones, desksByZone);
                }
            }
        }

        private static void AssignFallbackZones(List<Reservation> reservations, Dictionary<int, Queue<Desk>> desksByZone, Dictionary<int, List<Zone>> zonesByType, List<string> failed)
        {
            var unassignedReservations = reservations.Where(r => r.AssignedDeskId == null).ToList();

            var groupedByTeam = unassignedReservations
                .GroupBy(r => r.User.Team)
                .OrderByDescending(g => g.Count());

            foreach (var teamGroup in groupedByTeam)
            {
                var team = teamGroup.Key;
                var teamReservations = teamGroup.ToList();
                int teamSize = teamReservations.Count;

                var teamPreference = teamReservations
                    .GroupBy(r => r.ZonePreference)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                var fallbackTypes = Enumerable.Range(1, 4).Where(p => p != teamPreference);

                bool assigned = false;

                foreach (var fallbackType in fallbackTypes)
                {
                    var fallbackZones = zonesByType.GetValueOrDefault(fallbackType, new List<Zone>()).OrderBy(z => z.Id);

                    if (TryAssignWholeTeam(teamReservations, teamSize, fallbackZones, desksByZone))
                    {
                        assigned = true;
                        break;
                    }
                }

                if (assigned) continue;

                var fallbackQueue = new Queue<Reservation>(teamReservations.OrderBy(r => r.CreatedAt));

                foreach (var fallbackType in fallbackTypes)
                {
                    var fallbackZones = zonesByType.GetValueOrDefault(fallbackType, new List<Zone>()).OrderBy(z => z.Id);

                    foreach (var zone in fallbackZones)
                    {
                        if (!desksByZone.TryGetValue(zone.Id, out var availableDesks))
                            continue;

                        while (availableDesks.Count > 0 && fallbackQueue.Count > 0)
                        {
                            var res = fallbackQueue.Dequeue();
                            var desk = availableDesks.Dequeue();
                            res.AssignedDeskId = desk.Id;
                        }

                        if (fallbackQueue.Count == 0)
                            break;
                    }

                    if (fallbackQueue.Count == 0)
                        break;
                }

                if (fallbackQueue.Count > 0)
                {
                    failed.Add($"Team {team.name} (unassigned {fallbackQueue.Count}/{teamReservations.Count})");
                }
            }
        }

        private static bool TryAssignWholeTeam(List<Reservation> teamReservations, int teamSize, IEnumerable<Zone> zones, Dictionary<int, Queue<Desk>> desksByZone)
        {
            foreach (var zone in zones)
            {
                if (!desksByZone.TryGetValue(zone.Id, out var availableDesks))
                    continue;

                if (availableDesks.Count >= teamSize)
                {
                    for (int i = 0; i < teamSize; i++)
                    {
                        var res = teamReservations[i];
                        var desk = availableDesks.Dequeue();
                        res.AssignedDeskId = desk.Id;
                    }
                    return true;
                }
            }
            return false;
        }

        private static void AssignTeamIndividually(List<Reservation> teamReservations, IEnumerable<Zone> zones, Dictionary<int, Queue<Desk>> desksByZone)
        {
            var queue = new Queue<Reservation>(teamReservations.OrderBy(r => r.CreatedAt));

            foreach (var zone in zones)
            {
                if (!desksByZone.TryGetValue(zone.Id, out var availableDesks))
                    continue;

                while (availableDesks.Count > 0 && queue.Count > 0)
                {
                    var res = queue.Dequeue();
                    var desk = availableDesks.Dequeue();
                    res.AssignedDeskId = desk.Id;
                }

                if (queue.Count == 0)
                    break;
            }
        }
    }
}
