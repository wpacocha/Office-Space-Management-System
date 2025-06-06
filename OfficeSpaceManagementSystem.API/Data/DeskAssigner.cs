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

            var zones = desks
                .Select(d => d.Zone)
                .Where(z => z.Type != ZoneType.HR)
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

            var desksByZoneNonHr = desksByZone
                .Where(kvp => kvp.Key != hrZones.First())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            AssignExecutiveTeam(reservationsByTeam, reservations.Count, desksByZoneNonHr, executiveZones, failedAssignements);

            AssignSingleUsers(reservationsByTeam, desksByZoneNonHr, zones, singleTeamTypes, failedAssignements);
            AssignBestFitTeams(reservationsByTeam, desksByZoneNonHr, zones, otherTeamsTypes);

            AssignUsingMetaheuristic(reservationsByTeam, desksByZoneNonHr, otherTeamsTypes, failedAssignements);

            TryImproveDeskTypeMatch(reservationsByTeam, desks);

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
            int reservationsCount,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<string> preferredZones,
            List<string> failed)
        {
            desksByZone.TryGetValue(preferredZones.First(), out var execRoomDesksInput);
            var execRoomDesksCount = execRoomDesksInput!.Count;

            var executiveTeam = reservationsByTeam.Keys.FirstOrDefault(t => t.name == "Executive");
            if (executiveTeam == null) return;

            var executiveReservations = reservationsByTeam[executiveTeam];

            int assigned = AssignToZonesSequentially(executiveReservations, preferredZones, desksByZone);

            if (assigned < executiveReservations.Count)
                failed.Add($"Executive Team: {executiveReservations.Count - assigned} users could not be assigned.");

            if (desksByZone.TryGetValue(preferredZones.First(), out var execRoomDesks)
                && execRoomDesks.Count != execRoomDesksCount
                && reservationsCount - assigned <= 223 - execRoomDesksCount)
            {
                execRoomDesks.Clear();
            }

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
                .Where(kvp => kvp.Key.name.StartsWith("Solo"))
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
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            Dictionary<string, Queue<Desk>> desksByZone,
            List<ZoneType> typeOrder,
            List<string> failed)
        {
            var desks = desksByZone.Values.SelectMany(q => q).ToList();
            var reservations = reservationsByTeam.Values.SelectMany(q => q).ToList();

            var simulatedAnnealing = new SimulatedAnnealingAssigner();
            var optimizedReservations = simulatedAnnealing.Run(reservations, desks, typeOrder);
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

            if (failed.Count > 0)
                return;

            var desksByZoneList = desksByZone.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );

            var deskById = desksByZoneList
                .SelectMany(kvp => kvp.Value)
                .ToDictionary(d => d.Id, d => d);

            var reservationsByZone = new Dictionary<string, Dictionary<Team, List<Reservation>>>();

            foreach (var reservation in reservations)
            {
                if (!deskById.TryGetValue((int)reservation.AssignedDeskId, out var desk)) continue;

                var zone = desk.Zone.Name;
                var team = reservation.User.Team;

                if (!reservationsByZone.TryGetValue(zone, out var teamDict))
                {
                    teamDict = new Dictionary<Team, List<Reservation>>();
                    reservationsByZone[zone] = teamDict;
                }

                if (!teamDict.TryGetValue(team, out var list))
                {
                    list = new List<Reservation>();
                    teamDict[team] = list;
                }

                list.Add(reservation);
            }

            ReorganizeSeating(reservationsByZone, desksByZoneList);
        }

        private static void TryImproveDeskTypeMatch(
            Dictionary<Team, List<Reservation>> reservationsByTeam,
            List<Desk> allDesks)
        {
            var deskById = allDesks.ToDictionary(d => d.Id);

            foreach (var (team, reservations) in reservationsByTeam)
            {
                if (reservations.Count <= 1) continue;

                var teamReservations = reservations
                    .Where(r => r.AssignedDeskId != null)
                    .Select(r => (reservation: r, desk: deskById[r.AssignedDeskId!.Value]))
                    .ToList();

                for (int i = 0; i < teamReservations.Count; i++)
                {
                    var (reservationA, deskA) = teamReservations[i];
                    if (deskA.DeskType == reservationA.DeskTypePref)
                        continue;

                    for (int j = i + 1; j < teamReservations.Count; j++)
                    {
                        var (reservationB, deskB) = teamReservations[j];
                        if (deskB.DeskType == reservationB.DeskTypePref)
                            continue;

                        if (deskB.DeskType == reservationA.DeskTypePref && deskA.DeskType == reservationB.DeskTypePref)
                        {
                            (reservationA.AssignedDeskId, reservationB.AssignedDeskId) = (reservationB.AssignedDeskId, reservationA.AssignedDeskId);
                            break;
                        }
                    }
                }
            }
        }

        private static void ReorganizeSeating(
            Dictionary<string, Dictionary<Team, List<Reservation>>> reservationsByZone,
            Dictionary<string, List<Desk>> desksByZone)
        {
            foreach (var zoneKvp in reservationsByZone)
            {
                var zone = zoneKvp.Key;
                var teamsInZone = zoneKvp.Value;

                var desks = desksByZone[zone].OrderBy(d => d.Id).ToList();

                var deskToReservation = desks
                    .SelectMany(d => reservationsByZone[zone]
                        .SelectMany(kvp => kvp.Value)
                        .Where(r => r.AssignedDeskId == d.Id)
                        .Select(r => (deskId: d.Id, reservation: r)))
                    .ToDictionary(x => x.deskId, x => x.reservation);

                var reservationsInOrder = desks
                    .Where(d => deskToReservation.TryGetValue(d.Id, out _))
                    .Select(d => deskToReservation[d.Id])
                    .ToList();

                for (int i = 0; i < reservationsInOrder.Count; i++)
                {
                    var current = reservationsInOrder[i];
                    var currentTeam = current.User.Team;

                    int chainStart = i;
                    while (chainStart > 0 && reservationsInOrder[chainStart - 1].User.Team == currentTeam)
                    {
                        chainStart--;
                    }

                    var chainLength = i - chainStart + 1;
                    int teamSize = reservationsByZone[zone][currentTeam].Count;

                    if (chainLength >= teamSize)
                        continue;

                    for (int j = i + 1; j < reservationsInOrder.Count; j++)
                    {
                        if (reservationsInOrder[j].User.Team == currentTeam)
                        {
                            var targetIndex = i + 1;

                            var r1 = reservationsInOrder[j];
                            var r2 = reservationsInOrder[targetIndex];

                            (r1.AssignedDeskId, r2.AssignedDeskId) = (r2.AssignedDeskId, r1.AssignedDeskId);
                            (reservationsInOrder[j], reservationsInOrder[targetIndex]) = (reservationsInOrder[targetIndex], reservationsInOrder[j]);

                            break;
                        }
                    }
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
