using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using Xunit;

namespace OfficeSpaceManagementSystem.Tools.Exporters
{
    public sealed record AssignmentStat(
        int Iteration,
        string TeamName,
        int ReservationCount,
        int ZonesCount,
        int FloorsCount);

    public static class DeskAssignmentExporter
    {
        public static async Task RunAsync(
            int iterations,
            string csvPath,
            Func<int, SeedOptions> optionsFactory)
        {
            var allStats = new List<AssignmentStat>(capacity: iterations * 20);

            for (int i = 0; i < iterations; i++)
            {
                var options = optionsFactory(i);

                await using var context = CreateInMemoryContext();
                DbSeeder.Seed(context, options);

                var assigner = new DeskAssigner(context);
                var failed = await assigner.AssignAsync(options.ReservationDate);

                Assert.Empty(failed);

                var reservations = context.Reservations
                    .Include(r => r.User)
                        .ThenInclude(u => u.Team)
                    .Include(r => r.assignedDesk)
                        .ThenInclude(d => d.Zone)
                    .Where(r => r.Date == options.ReservationDate)
                    .ToList();

                var stats = reservations
                    .GroupBy(r => r.User.Team)
                    .Select(g =>
                    {
                        var zones = g
                        .Select(r => r.assignedDesk!.Zone.Name)
                        .Distinct()
                        .Count();

                        var floors = g
                            .Select(r => r.assignedDesk!.Zone.Florr)
                            .Distinct()
                            .Count();

                        return new AssignmentStat(
                            Iteration: i + 1,
                            TeamName: g.Key.name,
                            ReservationCount: g.Count(),
                            ZonesCount: zones,
                            FloorsCount: floors);
                    });

                allStats.AddRange(stats);
            }

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                NewLine = Environment.NewLine,
                Delimiter = ";"
            };

            await using var writer = new StreamWriter(csvPath, append: false);
            await using var csv = new CsvWriter(writer, csvConfig);

            await csv.WriteRecordsAsync(allStats);
            Console.WriteLine($"Zapisano {allStats.Count} wierszy do {csvPath}");
        }

        private static AppDbContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var ctx = new AppDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }
    }
}
