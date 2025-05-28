using OfficeSpaceManagementSystem.API.Models;
using OfficeSpaceManagementSystem.API.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OfficeSpaceManagementSystem.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db, SeedOptions? options = null)
        {
            options ??= new SeedOptions();

            db.Reservations.RemoveRange(db.Reservations);
            db.Desks.RemoveRange(db.Desks);
            db.Users.RemoveRange(db.Users);
            db.Teams.RemoveRange(db.Teams);
            db.SaveChanges();

            if (!db.Zones.Any())
                SeedZones(db);

            SeedTeams(db, options);
            SeedUsers(db, options);
            SeedDesks(db);
            ReservationGenerator.Generate(db, options.ReservationDate, options.ReservationsCount);
        }

        private static void SeedZones(AppDbContext db)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "zone_config.json");
            var zones = ZoneConfigLoader.LoadZones(path);
            db.Zones.AddRange(zones);
            db.SaveChanges();
        }

        private static void SeedTeams(AppDbContext db, SeedOptions options)
        {
            var teams = new List<Team>();

            // Solo 1–22
            for (int i = 1; i <= 22; i++)
                teams.Add(new Team { name = $"Solo {i}" });

            // HR & Executive
            teams.Add(new Team { name = "HR" });
            teams.Add(new Team { name = "Executive" });

            // Team A, B, ..., Z, AA, AB, ..., ZZ
            int toGenerate = options.TotalTeams - teams.Count;
            var nameSet = new HashSet<string>(teams.Select(t => t.name));

            int index = 0;
            while (teams.Count < options.TotalTeams)
            {
                string name = GenerateTeamName(index);
                if (!nameSet.Contains(name))
                {
                    teams.Add(new Team { name = name });
                    nameSet.Add(name);
                }
                index++;
            }

            db.Teams.AddRange(teams);
            db.SaveChanges();
        }

        private static string GenerateTeamName(int index)
        {
            string name = "";
            do
            {
                name = (char)('A' + index % 26) + name;
                index = index / 26 - 1;
            } while (index >= 0);
            return "Team " + name;
        }

        private static void SeedUsers(AppDbContext db, SeedOptions options)
        {
            var random = new Random();
            var teams = db.Teams.ToList();
            var users = new List<User>();
            int id = 1;

            foreach (var team in teams)
            {
                int count;

                if (team.name == "HR") count = 8;
                else if (team.name == "Executive") count = 16;
                else if (team.name.StartsWith("Solo")) count = 1;
                else count = random.Next(options.MinUsersPerTeam, options.MaxUsersPerTeam + 1);

                for (int i = 0; i < count && users.Count < options.TotalUsers; i++)
                {
                    users.Add(new User
                    {
                        EmployeeId = $"EMP{id:000}",
                        Email = $"user{id}@example.com",
                        Name = $"User {id}",
                        TeamId = team.Id
                    });
                    id++;
                }

                if (users.Count >= options.TotalUsers)
                    break;
            }

            db.Users.AddRange(users);
            db.SaveChanges();
        }

        private static void SeedDesks(AppDbContext db)
        {
            var zones = db.Zones.ToList();
            var desks = new List<Desk>();

            foreach (var zone in zones)
            {
                int total = zone.TotalDesks;
                int wideCount = zone.WideMonitorDesks;
                int dualCount = zone.DualMonitorDesks;

                var types = Enumerable.Repeat(DeskType.WideMonitor, wideCount)
                    .Concat(Enumerable.Repeat(DeskType.DualMonitor, dualCount))
                    .OrderBy(_ => Guid.NewGuid())
                    .ToList();

                for (int i = 0; i < types.Count; i++)
                {
                    desks.Add(new Desk
                    {
                        ZoneId = zone.Id,
                        DeskType = types[i],
                        Name = $"{zone.Florr}-{zone.Name.Split('-')[1]}-{i}"
                    });
                }
            }

            db.Desks.AddRange(desks);
            db.SaveChanges();
        }

    }
}
