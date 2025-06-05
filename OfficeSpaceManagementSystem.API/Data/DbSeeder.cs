using OfficeSpaceManagementSystem.API.Models;
using OfficeSpaceManagementSystem.API.Loaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OfficeSpaceManagementSystem.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db, SeedOptions? options = null)
        {
            options ??= new SeedOptions();
            var date = options.ReservationDate;

            Console.WriteLine($"[SEED] Removing reservations for {date}...");
            var reservationsToRemove = db.Reservations.Where(r => r.Date == date);
            db.Reservations.RemoveRange(reservationsToRemove);
            db.SaveChanges();

            if (!db.Zones.Any())
            {
                Console.WriteLine("[SEED] Seeding Zones...");
                SeedZones(db);
            }

            if (!db.Teams.Any())
            {
                Console.WriteLine("[SEED] Seeding Teams...");
                SeedTeams(db, options);
            }

            if (!db.Users.Any())
            {
                Console.WriteLine("[SEED] Seeding Users...");
                SeedUsers(db, options);
            }

            if (!db.Desks.Any())
            {
                Console.WriteLine("[SEED] Seeding Desks...");
                SeedDesks(db);
            }

            Console.WriteLine($"[SEED] Generating {options.ReservationsCount} reservations for {date}...");
            ReservationGenerator.Generate(db, date, options.ReservationsCount, options.FocusModePercentage);
            Console.WriteLine($"[SEED] ✅ Done.");
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

            for (int i = 1; i <= 22; i++)
                teams.Add(new Team { name = $"Solo {i}" });

            teams.Add(new Team { name = "HR" });
            teams.Add(new Team { name = "Executive" });

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
                int count = team.name switch
                {
                    "HR" => 8,
                    "Executive" => 16,
                    var solo when solo.StartsWith("Solo") => 1,
                    _ => random.Next(options.MinUsersPerTeam, options.MaxUsersPerTeam + 1)
                };

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
                var types = Enumerable.Repeat(DeskType.WideMonitor, zone.WideMonitorDesks)
                    .Concat(Enumerable.Repeat(DeskType.DualMonitor, zone.DualMonitorDesks))
                    .OrderBy(_ => Guid.NewGuid()) // shuffle
                    .ToList();

                if (types.Count != zone.TotalDesks)
                {
                    Console.WriteLine($"[WARN] Mismatch in zone {zone.Name}: expected {zone.TotalDesks}, got {types.Count}");
                }

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
            Console.WriteLine($"[SEED] Desks seeded: {desks.Count}");
        }
    }

}
