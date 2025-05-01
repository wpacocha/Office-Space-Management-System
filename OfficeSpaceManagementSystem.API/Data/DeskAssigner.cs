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
                .GroupBy(d => d.Zone.Name)
                .ToDictionary(g => g.Key, g => new Queue<Desk>(g.ToList()));

            var zones = desks.Select(d => d.Zone)
                .Distinct()
                .ToList();

            var reservationsByTeam = reservations
                .GroupBy(r => r.User.Team)
                .ToDictionary(g => g.Key, g => g.ToList());

            var failedAssignements = new List<string>();

            AssignHrTeam(reservationsByTeam, desksByZone, failedAssignements);
            AssignExecutiveTeam(reservationsByTeam, desksByZone, zones, failedAssignements);
            AssignDuoTeams(reservationsByTeam, desksByZone, zones);
            AssignSingleUsers(reservationsByTeam, desksByZone, zones, failedAssignements);
            AssignBestFitTeams(reservationsByTeam, desksByZone);
            //AssignBruteForce(reservationsByTeam, desksByZone, zones, failedAssignements);

            await _context.SaveChangesAsync();
            return failedAssignements;
        }

        private static void AssignHrTeam(
            Dictionary<Team, List<Reservation>> reservationsByTeam, 
            Dictionary<string, Queue<Desk>> desksByZone, 
            List<string> failed)
        {
            var hrTeam = reservationsByTeam.Keys.FirstOrDefault(t => t.name == "HR");
            if (hrTeam == null) return;

            var hrReservations = reservationsByTeam[hrTeam];
            string[] preferredZones = { "0-8", "0-7", "0-6", "0-5" };

            int assigned = AssignToZonesSequentially(hrReservations, preferredZones.ToList(), desksByZone);

            if (assigned < hrReservations.Count)
                failed.Add($"HR Team: {hrReservations.Count - assigned} users could not be assigned.");

            reservationsByTeam.Remove(hrTeam);
        }

        private static void AssignExecutiveTeam(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<Zone> allZones,
            List<string> failed)
        {
            var executiveTeam = reservationsByTeam.Keys.FirstOrDefault(t => t.name == "Executive");
            if (executiveTeam == null) return;

            var executiveReservations = reservationsByTeam[executiveTeam];

            var executiveZones = allZones
                .Where(z => z.Priority == 4)
                .OrderBy(z => z.Id)
                .Select(z => z.Name)
                .ToList();

            int assigned = AssignToZonesSequentially(executiveReservations, executiveZones, desksByZone);

            if (assigned < executiveReservations.Count)
                failed.Add($"Executive Team: {executiveReservations.Count - assigned} users could not be assigned.");

            reservationsByTeam.Remove(executiveTeam);
        }

        private static void AssignDuoTeams(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<Zone> allZones)
        {
            var duoFocusZones = allZones
                .Where(z => z.Priority == 3 && desksByZone.TryGetValue(z.Name, out var q) && q.Count == 2)
                .Select(z => new
                {
                    Zone = z,
                    Desks = new Queue<Desk>(desksByZone[z.Name])
                })
                .ToList();

            foreach (var kvp in reservationsByTeam.Where(kvp => kvp.Value.Count == 2).ToList())
            {
                var team = kvp.Key;
                var reservations = kvp.Value;

                var match = duoFocusZones.FirstOrDefault();
                if (match == null)
                    continue;

                reservations[0].AssignedDeskId = match.Desks.Dequeue().Id;
                reservations[1].AssignedDeskId = match.Desks.Dequeue().Id;

                desksByZone[match.Zone.Name] = match.Desks;
                reservationsByTeam.Remove(team);
                duoFocusZones.Remove(match);
            }
        }

        private static void AssignSingleUsers(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<Zone> allZones,
            List<string> failed)
        {
            var onePersonTeams = reservationsByTeam
                .Where(kvp => kvp.Value.Count == 1)
                .ToList();

            int[] priorityOrder = { 1, 2, 3, 4 };

            foreach (var (team, reservations) in onePersonTeams)
            {
                bool assigned = false;

                foreach (int priority in priorityOrder)
                {
                    var zones = allZones
                        .Where(z => z.Priority == priority && desksByZone.TryGetValue(z.Name, out var q) && q.Count > 0)
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
            Dictionary<string, Queue<Desk>> desksByZone)
        {
            var remainingTeams = reservationsByTeam.ToList();

            foreach (var (team, reservations) in remainingTeams)
            {
                int teamSize = reservations.Count;

                var matchingZone = desksByZone.FirstOrDefault(kvp => kvp.Value.Count == teamSize);

                if (matchingZone.Value != null && matchingZone.Value.Count == teamSize)
                {
                    var deskQueue = matchingZone.Value;

                    for (int i = 0; i < teamSize; i++)
                    {
                        reservations[i].AssignedDeskId = deskQueue.Dequeue().Id;
                    }

                    desksByZone[matchingZone.Key] = deskQueue;
                    reservationsByTeam.Remove(team);
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
