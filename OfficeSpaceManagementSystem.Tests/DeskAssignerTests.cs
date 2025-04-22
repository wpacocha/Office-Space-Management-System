using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using Xunit;
using Xunit.Abstractions;

namespace OfficeSpaceManagementSystem.Tests
{
    public class DeskAssignerTests
    {
        private readonly ITestOutputHelper _output;

        public DeskAssignerTests(ITestOutputHelper output)
        {
            _output = output;
        }


        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AssignAsync_ShouldAssignDesksCorrectly()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context);

            var deskAssigner = new DeskAssigner(context);
            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);

            var reservations = context.Reservations.Where(r => r.Date == date).ToList();
            Assert.All(reservations, r => Assert.NotNull(r.AssignedDeskId));

            Assert.Empty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_AssignMaxUsersStandardPreferences()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context, new SeedOptions { TotalUsers = 1000, ReservationsCount = 223 });
            var deskAssigner = new DeskAssigner(context);

            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);

            var reservations = context.Reservations.Where(r => r.Date == date).ToList();
            Assert.All(reservations, r => Assert.NotNull(r.AssignedDeskId));

            Assert.Empty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_AssignAllToPreferedZonesWhenPossible()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context, new SeedOptions { 
                TotalUsers = 1000, 
                ReservationsCount = 223,
                ZonePreferenceSelector = i =>
                {
                    if (i < 142)
                        return 1;
                    else if (i < 181)
                        return 2;
                    else if (i < 207)
                        return 3;
                    else
                        return 4;
                }
            });
            var deskAssigner = new DeskAssigner(context);

            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations.Where(r => r.Date == date).ToList();

            foreach (var reservation in reservations)
            {
                var desk = context.Desks.Find(reservation.AssignedDeskId);
                Assert.NotNull(desk);
                var zone = context.Zones.Find(desk.ZoneId);
                Assert.NotNull(zone);
                Assert.Equal(reservation.ZonePreference, zone.Priority);
            }

            Assert.Empty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_ShouldAccountForDeskPreference()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context);

            var deskAssigner = new DeskAssigner(context);
            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);

            var reservations = context.Reservations.Where(r => r.Date == date).ToList();
            foreach (var reservation in reservations)
            {
                var desk = context.Desks.Find(reservation.AssignedDeskId);
                Assert.NotNull(desk);
                Assert.Equal(reservation.DeskTypePref, desk.DeskType);
            }

            Assert.Empty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_ShouldReturnFailed_WhenNotAllCanBeAssigned()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context, new SeedOptions { ReservationsCount = 224 });

            var deskAssigner = new DeskAssigner(context);
            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations.Where(r => r.Date == date).ToList();
            
            Assert.NotEmpty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_ShouldSplitTeamsAcrossPreferredZones()
        {
            using var context = GetInMemoryContext();
            DbSeeder.Seed(context, new SeedOptions
            {
                TotalUsers = 1000,
                ReservationsCount = 223,
                MinUsersPerTeam = 31,
                MaxUsersPerTeam = 40,
                ZonePreferenceSelector = i =>
                {
                    if (i < 142)
                        return 1;
                    else if (i < 181)
                        return 2;
                    else if (i < 207)
                        return 3;
                    else
                        return 4;
                }
            });

            var deskAssigner = new DeskAssigner(context);
            var date = new DateOnly(2024, 4, 25);

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations.Where(r => r.Date == date).ToList();

            foreach (var reservation in reservations)
            {
                var desk = context.Desks.Find(reservation.AssignedDeskId);
                Assert.NotNull(desk);
                var zone = context.Zones.Find(desk.ZoneId);
                Assert.NotNull(zone);
                Assert.Equal(reservation.ZonePreference, zone.Priority);
            }
            Assert.Empty(failedTeams);
        }
    }
}
