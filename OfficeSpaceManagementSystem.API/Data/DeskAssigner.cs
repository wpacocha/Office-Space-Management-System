using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Loaders;
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
                .GroupBy(d => d.Zone.Name)
                .ToDictionary(g => g.Key, g => new Queue<Desk>(g.ToList()));

            var zones = desks.Select(d => d.Zone)
                .Distinct()
                .ToList();

            var reservationsByTeam = reservations
                .GroupBy(r => r.User.Team)
                .ToDictionary(g => g.Key, g => g.ToList());

            var failedAssignements = new List<string>();

            var path = Path.Combine(AppContext.BaseDirectory, "assignment_config.json");
            var config = AssignmentConfigLoader.Load(path);

            var hrZones = config.SpecialTeams["HR"].ToList();
            var executiveZones = config.SpecialTeams["Executive"].ToList();

            var singleTeamTypes = config.TeamSizeRules
                .Where(r => r.MinSize == 1 && r.MaxSize == 1)
                .SelectMany(r => r.PriorityTypes)
                .Select(s => Enum.Parse<ZoneType>(s))
                .ToList();

            var otherTeamsTypes = config.TeamSizeRules
                .Where(r => r.MinSize > 1)
                .SelectMany(r => r.PriorityTypes)
                .Select(s => Enum.Parse<ZoneType>(s))
                .ToList();

            AssignHrTeam(reservationsByTeam, desksByZone, hrZones, failedAssignements);
            AssignExecutiveTeam(reservationsByTeam, desksByZone, executiveZones, failedAssignements);

            AssignSingleUsers(reservationsByTeam, desksByZone, zones, singleTeamTypes, failedAssignements);
            AssignBestFitTeams(reservationsByTeam, desksByZone, zones, otherTeamsTypes);

            await _context.SaveChangesAsync();

            reservations = await _context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date)
                .ToListAsync();

            var freeDesks = desksByZone.Values.SelectMany(q => q).ToList();
            var unassignedReservations = reservationsByTeam.Values.SelectMany(q => q).ToList();

            AssignUsingMetaheuristic(unassignedReservations, freeDesks, failedAssignements);

            await _context.SaveChangesAsync();
            return failedAssignements;
        }

        private static void AssignHrTeam(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<string> preferredZones,
            List<string> failed)
        {
            var hrTeam = reservationsByTeam.Keys.FirstOrDefault(t => t.name == "HR");
            if (hrTeam == null) return;

            var hrReservations = reservationsByTeam[hrTeam];

            int assigned = AssignToZonesSequentially(hrReservations, preferredZones, desksByZone);

            if (assigned < hrReservations.Count)
                failed.Add($"HR Team: {hrReservations.Count - assigned} users could not be assigned.");

            reservationsByTeam.Remove(hrTeam);
        }

        private static void AssignExecutiveTeam(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<string> preferredZones,
            List<string> failed)
        {
            var executiveTeam = reservationsByTeam.Keys.FirstOrDefault(t => t.name == "Executive");
            if (executiveTeam == null) return;

            var executiveReservations = reservationsByTeam[executiveTeam];

            int assigned = AssignToZonesSequentially(executiveReservations, preferredZones, desksByZone);

            if (assigned < executiveReservations.Count)
                failed.Add($"Executive Team: {executiveReservations.Count - assigned} users could not be assigned.");

            reservationsByTeam.Remove(executiveTeam);
        }

        private static void AssignSingleUsers(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<Zone> allZones,
            List<ZoneType> typeOrder,
            List<string> failed)
        {
            var onePersonTeams = reservationsByTeam
                .Where(kvp => kvp.Value.Count == 1)
                .ToList();

            foreach (var (team, reservations) in onePersonTeams)
            {
                bool assigned = false;

                foreach (var type in typeOrder)
                {
                    var zones = allZones
                        .Where(z => z.Type == type && desksByZone.TryGetValue(z.Name, out var q) && q.Count > 0)
                        .ToList();

                    foreach (var zone in zones)
                    {
                        var deskQueue = desksByZone[zone.Name];
                        if (deskQueue.Count == 0) continue;

                        reservations[0].AssignedDeskId = deskQueue.Dequeue().Id;
                        assigned = true;
                        desksByZone[zone.Name] = deskQueue;
                        break;
                    }

                    if (assigned) break;
                }

                if (assigned)
                    reservationsByTeam.Remove(team);
                else
                    failed.Add($"Team {team.name}: no desk could be assigned");
            }
        }

        private static void AssignBestFitTeams(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<Zone> allZones,
            List<ZoneType> typeOrder)
        {          
            var emptyZones = allZones
                .Where(z => z.TotalDesks == desksByZone[z.Name].Count)
                .OrderBy(z => typeOrder.IndexOf(z.Type))
                .ToList();

            foreach (var (team, reservations) in reservationsByTeam.ToList())
            {
                int teamSize = reservations.Count;

                var matchingZone = emptyZones
                    .FirstOrDefault(z => z.TotalDesks == teamSize);

                if (matchingZone != null)
                {
                    var desksToAssign = desksByZone[matchingZone.Name];

                    for (int i = 0; i < teamSize; i++)
                    {
                        reservations[i].AssignedDeskId = desksToAssign.Dequeue().Id;
                    }

                    emptyZones.Remove(matchingZone);
                    reservationsByTeam.Remove(team);
                }
            }
        }

        private static void AssignUsingMetaheuristic(
            List<Reservation> reservations,
            List<Desk> desks,
            List<string> failed)
        {
            var simulatedAnnealing = new SimulatedAnnealingAssigner();
            var optimizedReservations = simulatedAnnealing.Run(reservations, desks);
            for (int i = 0; i < reservations.Count; i++)
            {
                if (reservations[i].AssignedDeskId != null) continue;

                if (optimizedReservations[i].AssignedDeskId == null)
                {
                    failed.Add($"Reservation {reservations[i].Id} could not be assigned.");
                }
                else
                {
                    reservations[i].AssignedDeskId = optimizedReservations[i].AssignedDeskId;
                }
            }
        }

        private static int AssignToZonesSequentially(
            List<Reservation> reservations,
            List<string> zoneNames,
            Dictionary<string, Queue<Desk>> desksByZone)
        {
            int assigned = 0;
            foreach (var zoneName in zoneNames)
            {
                if (!desksByZone.TryGetValue(zoneName, out var availableDesks) || availableDesks.Count == 0)
                    continue;

                while (availableDesks.Count > 0 && assigned < reservations.Count)
                {
                    reservations[assigned].AssignedDeskId = availableDesks.Dequeue().Id;
                    assigned++;
                }

                if (assigned == reservations.Count)
                    break;
            }

            return assigned;
        }
    }
}
