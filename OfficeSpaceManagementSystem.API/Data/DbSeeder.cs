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
            db.Database.EnsureDeleted();     // 💥 Resetuje całą bazę (schema + dane)
            db.Database.EnsureCreated();

            options ??= new SeedOptions();

            SeedZones(db);
            SeedTeams(db, options);
            SeedUsers(db, options);
            SeedDesks(db);
            SeedReservations(db, options);
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

            // Solo teams
            for (int i = 1; i <= 50; i++)
                teams.Add(new Team { name = $"Solo {i}" });

            // HR & Executive
            teams.Add(new Team { name = "HR" });
            teams.Add(new Team { name = "Executive" });

            // Remaining teams
            int toGenerate = options.TotalTeams - teams.Count;
            char letter = 'A';
            for (int i = 0; i < toGenerate; i++)
            {
                string name = $"Team {letter}";
                if (i >= 26)
                    name = $"Team {letter}{(char)('A' + (i - 26) % 26)}";

                teams.Add(new Team { name = name });
                if ((i + 1) % 26 == 0) letter++;
            }

            db.Teams.AddRange(teams);
            db.SaveChanges();
        }

        private static void SeedUsers(AppDbContext db, SeedOptions options)
        {
            var random = new Random();
            var teams = db.Teams.ToList();
            var users = new List<User>();
            int id = 1;

            foreach (var team in teams)
            {
                int count = 1;

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
                DeskType currentType = zone.FirstDeskType;

                for (int i = 0; i < zone.TotalDesks; i++)
                {
                    desks.Add(new Desk
                    {
                        ZoneId = zone.Id,
                        DeskType = currentType,
                        Name = $"{zone.Florr}-{zone.Name.Split('-')[1]}-{i}"
                    });

                    currentType = currentType == DeskType.WideMonitor ? DeskType.DualMonitor : DeskType.WideMonitor;
                }
            }

            db.Desks.AddRange(desks);
            db.SaveChanges();
        }

        private static void SeedReservations(AppDbContext db, SeedOptions options)
        {
            var users = db.Users.ToList().OrderBy(_ => Guid.NewGuid()).Take(options.ReservationsCount).ToList();
            var reservations = new List<Reservation>();

            for (int i = 0; i < users.Count; i++)
            {
                var type = options.DeskTypeSelector?.Invoke(i) ?? (DeskType)(i % 2);
                var zone = options.ZonePreferenceSelector?.Invoke(i) ?? new Random().Next(1, 5);

                reservations.Add(new Reservation
                {
                    UserId = users[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = options.ReservationDate,
                    DeskTypePref = type,
                    isFocusMode = false,
                    assignedDesk = null
                });
            }

            db.Reservations.AddRange(reservations);
            db.SaveChanges();
        }
    }
}
