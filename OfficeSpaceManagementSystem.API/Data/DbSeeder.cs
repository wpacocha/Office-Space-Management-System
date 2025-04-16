using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (context.Zones.Any()) return; // dane ju¿ istniej¹

            // Strefy (Zone)
            var zones = new List<Zone>
            {
                new Zone { Name = "Strefa A", Priority = 1, Florr = 0, StandardDesks = 5, DualMonitorDesks = 5, SuperchargedDesks = 2, TotalDesks = 12 },
                new Zone { Name = "Strefa B", Priority = 2, Florr = 1, StandardDesks = 10, DualMonitorDesks = 5, SuperchargedDesks = 1, TotalDesks = 16 },
                new Zone { Name = "Strefa C", Priority = 3, Florr = 2, StandardDesks = 8, DualMonitorDesks = 4, SuperchargedDesks = 1, TotalDesks = 13 },
            };
            context.Zones.AddRange(zones);
            context.SaveChanges();

            // Biurka (Desk)
            int id = 1;
            foreach (var zone in zones)
            {
                for (int i = 0; i < zone.StandardDesks; i++)
                    context.Desks.Add(new Desk { Id = id++, Name = $"STD_{id}", ZoneId = zone.Id, DeskType = DeskType.Standard });
                for (int i = 0; i < zone.DualMonitorDesks; i++)
                    context.Desks.Add(new Desk { Id = id++, Name = $"DM_{id}", ZoneId = zone.Id, DeskType = DeskType.DualMonitor });
                for (int i = 0; i < zone.SuperchargedDesks; i++)
                    context.Desks.Add(new Desk { Id = id++, Name = $"SC_{id}", ZoneId = zone.Id, DeskType = DeskType.Supercharged });
            }
            context.SaveChanges();

            // Zespo³y
            var teams = new List<Team>
            {
                new Team { name = "Backend" },
                new Team { name = "Frontend" },
                new Team { name = "QA" }
            };
            context.Teams.AddRange(teams);
            context.SaveChanges();

            // U¿ytkownicy
            var users = new List<User>
            {
                new User { Name = "Anna", Email = "anna@firma.com", EmployeeId = "E1", TeamId = teams[0].Id },
                new User { Name = "Bartek", Email = "bartek@firma.com", EmployeeId = "E2", TeamId = teams[0].Id },
                new User { Name = "Celina", Email = "celina@firma.com", EmployeeId = "E3", TeamId = teams[1].Id },
                new User { Name = "Darek", Email = "darek@firma.com", EmployeeId = "E4", TeamId = teams[1].Id },
                new User { Name = "Ela", Email = "ela@firma.com", EmployeeId = "E5", TeamId = teams[1].Id },
                new User { Name = "Filip", Email = "filip@firma.com", EmployeeId = "E6", TeamId = teams[2].Id },
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // Rezerwacje na dziœ
            var today = DateOnly.FromDateTime(DateTime.Today);
            var reservations = users.Select(u => new Reservation
            {
                UserId = u.Id,
                Date = today,
                CreatedAt = DateTime.Now,
                DeskTypePref = DeskType.Standard,
                ZonePreference = 1
            }).ToList();

            context.Reservations.AddRange(reservations);
            context.SaveChanges();
        }
    }
}
