using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public static class ReservationGenerator
    {
        public static void Generate(AppDbContext db, DateOnly date, int count, double focusModePercentage)
        {
            var random = new Random();

            // POPRAWKA: dołączenie zespołu do użytkowników
            var users = db.Users
                .Include(u => u.Team)
                .ToList()
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            var soloUsers = users
                .Where(u => u.Team != null && u.Team.name.StartsWith("Solo"))
                .ToList();

            var reservations = new List<Reservation>();

            int maxFocus = Math.Min(soloUsers.Count, (int)(count * focusModePercentage));
            var focusUsers = soloUsers.Take(maxFocus).ToList();

            for (int i = 0; i < focusUsers.Count; i++)
            {
                reservations.Add(new Reservation
                {
                    UserId = focusUsers[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = date,
                    DeskTypePref = (DeskType)(i % 2),
                    isFocusMode = true,
                    AssignedDeskId = null
                });
            }

            int generalCount = count - reservations.Count;
            var focusUserIds = focusUsers.Select(u => u.Id).ToHashSet();
            var remainingUsers = users.Where(u => !focusUserIds.Contains(u.Id)).Take(generalCount).ToList();

            if (remainingUsers.Count < generalCount)
            {
                Console.WriteLine($"[WARN] Only {remainingUsers.Count} general users available for {generalCount} reservations.");
                generalCount = remainingUsers.Count;
            }

            for (int i = 0; i < generalCount; i++)
            {
                reservations.Add(new Reservation
                {
                    UserId = remainingUsers[i].Id,
                    CreatedAt = DateTime.Now,
                    Date = date,
                    DeskTypePref = (DeskType)(i % 2),
                    isFocusMode = false,
                    AssignedDeskId = null
                });
            }

            db.Reservations.AddRange(reservations);
            db.SaveChanges();

            Console.WriteLine($"[SEED] Generated reservations:");
            Console.WriteLine($" - Focus mode: {focusUsers.Count}");
            Console.WriteLine($" - General: {generalCount}");
            Console.WriteLine($" - Total: {reservations.Count}");
            Console.WriteLine($" - Desks in DB: {db.Desks.Count()}");
        }
    }
}
