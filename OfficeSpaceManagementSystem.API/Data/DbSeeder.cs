using OfficeSpaceManagementSystem.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfficeSpaceManagementSystem.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (!db.Zones.Any()) SeedZones(db);
            if (!db.Teams.Any()) SeedTeams(db);
            if (!db.Users.Any()) SeedUsers(db);
            if (!db.Desks.Any()) SeedDesks(db);
            if (!db.Reservations.Any()) SeedReservations(db);
        }

        private static void SeedZones(AppDbContext db)
        {
            var zones = new List<Zone>
            {
                // Parter (0)
                new Zone { Name = "0-1", Florr = 0, Priority = 1, StandardDesks = 15, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 15 },
                new Zone { Name = "0-2", Florr = 0, Priority = 2, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "0-3", Florr = 0, Priority = 4, StandardDesks = 6, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 6 },
                new Zone { Name = "0-4", Florr = 0, Priority = 4, StandardDesks = 4, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 4 },
                new Zone { Name = "0-5", Florr = 0, Priority = 4, StandardDesks = 6, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 6 },
                new Zone { Name = "0-6", Florr = 0, Priority = 2, StandardDesks = 6, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 6 },
                new Zone { Name = "0-7", Florr = 0, Priority = 1, StandardDesks = 4, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 4 },
                new Zone { Name = "0-8", Florr = 0, Priority = 1, StandardDesks = 4, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 4 },
                // Piętro 1
                new Zone { Name = "1-1", Florr = 1, Priority = 1, StandardDesks = 24, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 24 },
                new Zone { Name = "1-2", Florr = 1, Priority = 2, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "1-3", Florr = 1, Priority = 3, StandardDesks = 2, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 2 },
                new Zone { Name = "1-4", Florr = 1, Priority = 2, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "1-5", Florr = 1, Priority = 1, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "1-6", Florr = 1, Priority = 1, StandardDesks = 21, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 21 },
                new Zone { Name = "1-7", Florr = 1, Priority = 1, StandardDesks = 13, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 13 },
                // Piętro 2
                new Zone { Name = "2-1", Florr = 2, Priority = 1, StandardDesks = 18, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 18 },
                new Zone { Name = "2-2", Florr = 2, Priority = 3, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "2-3", Florr = 2, Priority = 3, StandardDesks = 2, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 2 },
                new Zone { Name = "2-4", Florr = 2, Priority = 3, StandardDesks = 9, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 9 },
                new Zone { Name = "2-5", Florr = 2, Priority = 2, StandardDesks = 6, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 6 },
                new Zone { Name = "2-6", Florr = 2, Priority = 1, StandardDesks = 4, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 4 },
                new Zone { Name = "2-7", Florr = 2, Priority = 1, StandardDesks = 30, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 30 },
                new Zone { Name = "2-8", Florr = 2, Priority = 3, StandardDesks = 4, DualMonitorDesks = 0, SuperchargedDesks = 0, TotalDesks = 4 }
            };

            db.Zones.AddRange(zones);
            db.SaveChanges();
        }

        private static void SeedTeams(AppDbContext db)
        {
            var teams = new List<Team>();
            for (int i = 1; i <= 98; i++) teams.Add(new Team { name = $"Team {i}" });
            teams.Add(new Team { name = "Executive" });
            teams.Add(new Team { name = "HR" });
            db.Teams.AddRange(teams);
            db.SaveChanges();
        }

        private static void SeedUsers(AppDbContext db)
        {
            var random = new Random();
            var teams = db.Teams.ToList();
            var users = new List<User>();
            int id = 1;
            while (users.Count < 300)
            {
                foreach (var team in teams)
                {
                    int count = random.Next(2, 31);
                    for (int i = 0; i < count && users.Count < 300; i++)
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
                }
            }
            db.Users.AddRange(users);
            db.SaveChanges();
        }

        private static void SeedDesks(AppDbContext db)
        {
            var zones = db.Zones.ToList();
            var random = new Random();
            var desks = new List<Desk>();
            int id = 1;

            var types = Enumerable.Repeat(DeskType.Supercharged, 111)
                .Concat(Enumerable.Repeat(DeskType.DualMonitor, 70))
                .Concat(Enumerable.Repeat(DeskType.Standard, 232 - 111 - 70))
                .OrderBy(_ => random.Next())
                .ToList();

            foreach (var zone in zones)
            {
                for (int i = 0; i < zone.TotalDesks; i++)
                {
                    var type = types.Count > 0 ? types[0] : DeskType.Standard;
                    if (types.Count > 0) types.RemoveAt(0);

                    desks.Add(new Desk
                    {
                        ZoneId = zone.Id,
                        DeskType = type,
                        Name = $"{zone.Florr}{i:D2}{zone.Priority}{type.ToString()[0].ToString().ToLower()}"
                    });
                }
            }

            db.Desks.AddRange(desks);
            db.SaveChanges();
        }

        private static void SeedReservations(AppDbContext db)
        {
            var users = db.Users.ToList().OrderBy(_ => Guid.NewGuid()).Take(200).ToList();
            var random = new Random();
            var types = Enumerable.Repeat(DeskType.Supercharged, 111)
                .Concat(Enumerable.Repeat(DeskType.DualMonitor, 70))
                .Concat(Enumerable.Repeat(DeskType.Standard, 19))
                .OrderBy(_ => random.Next())
                .ToList();

            var zones = Enumerable.Repeat(1, 138)
                .Concat(Enumerable.Repeat(2, 29))
                .Concat(Enumerable.Repeat(3, 26))
                .Concat(Enumerable.Repeat(4, 16))
                .OrderBy(_ => random.Next())
                .ToList();

            var reservations = new List<Reservation>();
            for (int i = 0; i < users.Count; i++)
            {
                reservations.Add(new Reservation
                {
                    UserId = users[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = new DateOnly(2024, 4, 25),
                    DeskTypePref = types[i],
                    ZonePreference = zones[i],
                    assignedDesk = null
                });
            }

            db.Reservations.AddRange(reservations);
            db.SaveChanges();
        }
    }
}