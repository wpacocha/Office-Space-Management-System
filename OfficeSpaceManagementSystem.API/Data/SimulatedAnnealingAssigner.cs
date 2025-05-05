using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class SimulatedAnnealingAssigner
    {
        private readonly Random _random = new Random();

        public List<Reservation> Run(
            List<Reservation> reservations,
            List<Desk> desks,
            int maxIterations = 100_000,
            double initialTemperature = 10_000.0,
            double endTemperature = 1.0,
            double coolingRate = 0.998)
        {
            var current = GenerateInitialSolution(reservations, desks);
            var best = Clone(current);

            double temperature = initialTemperature;
            int currentScore = CalculateScore(current, desks);
            int bestScore = currentScore;

            for (int i = 0; i < maxIterations && temperature > endTemperature; i++)
            {
                var neighbor = Mutate(Clone(current));
                int neighborScore = CalculateScore(neighbor, desks);

                if (neighborScore < currentScore || AcceptWorseSolution(currentScore, neighborScore, temperature))
                {
                    current = neighbor;
                    currentScore = neighborScore;

                    if (currentScore < bestScore)
                    {
                        best = Clone(current);
                        bestScore = currentScore;
                    }
                }

                temperature *= coolingRate;
            }

            return best;
        }

        private List<Reservation> GenerateInitialSolution(List<Reservation> reservations, List<Desk> desks)
        {
            var copy = Clone(reservations);
            var shuffledDesks = desks.OrderBy(_ => _random.Next()).ToList();
            var queue = new Queue<Desk>(shuffledDesks);

            foreach (var r in copy)
            {
                if (queue.Count > 0)
                {
                    r.AssignedDeskId = queue.Dequeue().Id;
                }
            }

            return copy;
        }

        private List<Reservation> Mutate(List<Reservation> reservations)
        {
            var mutable = reservations.Where(r => r.AssignedDeskId != null).ToList();
            if (mutable.Count < 2) return reservations;

            int i = _random.Next(mutable.Count);
            int j = _random.Next(mutable.Count);
            if (i == j) return reservations;

            var tempDeskId = mutable[i].AssignedDeskId;
            mutable[i].AssignedDeskId = mutable[j].AssignedDeskId;
            mutable[j].AssignedDeskId= tempDeskId;

            return reservations;
        }

        private static List<Reservation> Clone(List<Reservation> reservations)
        {
            return reservations.Select(r => new Reservation
            {
                Id = r.Id,
                UserId = r.UserId,
                User = r.User,
                Date = r.Date,
                CreatedAt = r.CreatedAt,
                ZonePreference = r.ZonePreference,
                DeskTypePref = r.DeskTypePref,
                AssignedDeskId = r.AssignedDeskId,
                assignedDesk = r.assignedDesk
            }).ToList();
        }

        private bool AcceptWorseSolution(int currentScore, int newScore, double temperature)
        {
            double probability = Math.Exp((currentScore - newScore) / temperature);
            return _random.NextDouble() < probability;
        }

        public static int CalculateScore(List<Reservation> reservations, List<Desk> desks)
        {
            int score = 0;

            //var groupedByZone = reservations
            //    .Where(r => r.AssignedDeskId != null)
            //    .GroupBy(r => desks.First(d => d.Id == r.AssignedDeskId).Zone);

            //foreach (var zoneGroup in groupedByZone)
            //{
            //    var zone = zoneGroup.Key;
            //    var reservationsInZone = zoneGroup.ToList();

            //    int prefWide = reservationsInZone.Count(r => r.DeskTypePref == DeskType.WideMonitor);
            //    int prefDual = reservationsInZone.Count(r => r.DeskTypePref == DeskType.DualMonitor);

            //    int deskWide = zone.Desks.Count(d => d.DeskType == DeskType.WideMonitor);
            //    int deskDual = zone.Desks.Count(d => d.DeskType == DeskType.DualMonitor);

            //    score += Math.Abs(prefWide - deskWide);
            //    score += Math.Abs(prefDual - deskDual);
            //}

            var reservationsByTeam = reservations
                .Where(r => r.AssignedDeskId != null)
                .GroupBy(r => r.User.Team.Id);

            foreach (var teamGroup in reservationsByTeam)
            {
                var floors = teamGroup
                    .Select(r => desks.First(d => d.Id == r.AssignedDeskId).Zone.Florr)
                    .Distinct()
                    .ToList();


                if (floors.Count > 1)
                {
                    score += 1_000_000;
                }

                var zones = teamGroup
                    .Select(r => desks.First(d => d.Id == r.AssignedDeskId).Zone.Id)
                    .Distinct()
                    .ToList();

                if (zones.Count > 1)
                {
                    int teamSize = teamGroup.Count();

                    if (zones.Count == 2)
                        score += 500 * teamSize;
                    else
                        score += 1000 * (zones.Count - 2) * teamSize;
                }
            }

            return score;
        }
    }
}
