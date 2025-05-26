using System;
using System.Collections.Generic;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public static class ReservationGenerator
    {
        public static void Generate(AppDbContext db, DateOnly date, int count)
        {
            var random = new Random();

            var users = db.Users.ToList().OrderBy(_ => Guid.NewGuid()).ToList();
            var soloUsers = users
                .Where(u => u.Team.name.StartsWith("Solo"))
                .Take(22)
                .ToList();

            var reservations = new List<Reservation>();

            int focusCount = Math.Min(22, count / 10); // np. 10% użytkowników z Focus mode
            for (int i = 0; i < focusCount && i < soloUsers.Count; i++)
            {
                reservations.Add(new Reservation
                {
                    UserId = soloUsers[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = date,
                    DeskTypePref = (DeskType)(i % 2),
                    ZonePreference = random.Next(1, 5),
                    isFocusMode = true,
                    AssignedDeskId = null
                });
            }

            int generalCount = count - focusCount;
            var remainingUsers = users.Except(soloUsers).Take(generalCount).ToList();

            for (int i = 0; i < remainingUsers.Count; i++)
            {
                reservations.Add(new Reservation
                {
                    UserId = remainingUsers[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = date,
                    DeskTypePref = (DeskType)(i % 2),
                    ZonePreference = random.Next(1, 5),
                    isFocusMode = false,
                    AssignedDeskId = null
                });
            }

            db.Reservations.AddRange(reservations);
            db.SaveChanges();
        }
    }
}
