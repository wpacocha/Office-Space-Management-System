using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using OfficeSpaceManagementSystem.API.Loaders;
using OfficeSpaceManagementSystem.API.Models;
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
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        [Fact]
        public async Task AssignAsync_ShouldAssignDesksCorrectly()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions();
            DbSeeder.Seed(context);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);

            var reservations = context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Include(r => r.assignedDesk)
                .ThenInclude(r => r.Zone)
                .Where(r => r.Date == date)
                .ToList();

            var reservationsByTeam = reservations
                .GroupBy(r => r.User.Team)
                .ToDictionary(g => g.Key, g => g.ToList())
                .OrderByDescending(kyp => kyp.Value.Count);

            foreach (var teamReservations in reservationsByTeam)
            {
                var zones = teamReservations.Value
                    .Select(r => r.assignedDesk!.Zone.Name)
                    .Distinct()
                    .ToList();

                _output.WriteLine($"{teamReservations.Key.name} - {zones.Count} zones");

                foreach (var reservation in teamReservations.Value)
                {
                    _output.WriteLine($"    Reservation {reservation.Id}; Zone {reservation.assignedDesk!.Zone.Name}; {reservation.assignedDesk.Name}");
                }
            }

            Assert.All(reservations, r => Assert.NotNull(r.AssignedDeskId));

            Assert.Empty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_AssignMaxUsersStandardPreferences()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions 
            {
                TotalUsers = 1000,
                ReservationsCount = 219
            };
            DbSeeder.Seed(context, options);
            var deskAssigner = new DeskAssigner(context);

            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);

            Assert.Empty(failedTeams);

            var reservations = context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Include(r => r.assignedDesk)
                .ThenInclude(r => r.Zone)
                .Where(r => r.Date == date)
                .ToList();

            var reservationsByTeam = reservations
                .GroupBy(r => r.User.Team)
                .ToDictionary(g => g.Key, g => g.ToList())
                .OrderByDescending(kyp => kyp.Value.Count);

            foreach (var teamReservations in reservationsByTeam)
            {
                var zones = teamReservations.Value
                    .Select(r => r.assignedDesk?.Zone.Name)
                    .Distinct()
                    .ToList();

                _output.WriteLine($"{teamReservations.Key.name} - {zones.Count} zones");

                foreach (var reservation in teamReservations.Value)
                {
                    _output.WriteLine($"    Reservation {reservation.Id}; Zone {reservation.assignedDesk!.Zone.Name}; {reservation.assignedDesk.Name}");
                }
            }

            Assert.All(reservations, r => Assert.NotNull(r.AssignedDeskId));
        }

        [Fact]
        public async Task AssignAsync_ShouldReturnFailed_WhenNotAllCanBeAssigned()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions { ReservationsCount = 224 };
            DbSeeder.Seed(context, options);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations.Where(r => r.Date == date).ToList();
            
            Assert.NotEmpty(failedTeams);
        }

        [Fact]
        public async Task AssignAsync_ShouldAssignHrToCorrectZones()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions();
            DbSeeder.Seed(context, options);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date && r.User.Team.name == "HR")
                .ToList();

            var path = Path.Combine(AppContext.BaseDirectory, "assignment_config.json");
            var config = AssignmentConfigLoader.Load(path);

            var hrZones = config.SpecialTeams["HR"].ToList();

            _output.WriteLine($"Found {reservations.Count} HR reservations.");
            foreach (var reservation in reservations)
            {
                var desk = context.Desks.Find(reservation.AssignedDeskId);
                var zone = context.Zones.Find(desk?.ZoneId);
                Assert.Contains(zone?.Name, hrZones);
            }
        }

        [Fact]
        public async Task AssignAsync_ShouldAssignExecutiveToCorrectZones()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions();
            DbSeeder.Seed(context, options);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date && r.User.Team.name == "Executive")
                .ToList();

            var path = Path.Combine(AppContext.BaseDirectory, "assignment_config.json");
            var config = AssignmentConfigLoader.Load(path);

            var executiveZones = config.SpecialTeams["Executive"].ToList();

            _output.WriteLine($"Found {reservations.Count} Executive reservations.");
            foreach (var reservation in reservations)
            {
                var desk = context.Desks.Find(reservation.AssignedDeskId);
                var zone = context.Zones.Find(desk?.ZoneId);
                Assert.Contains(zone?.Name, executiveZones);
            }
        }

        [Fact]
        public async Task AssignAsync_ShouldAssignSolos()
        {
            using var context = GetInMemoryContext();

            var options = new SeedOptions();
            DbSeeder.Seed(context, options);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);
            var reservations = context.Reservations
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Where(r => r.Date == date && r.User.Team.name.StartsWith("Solo"))
                .ToList();

            _output.WriteLine($"Found {reservations.Count} Solo reservations.");
            foreach (var reservation in reservations)
            {
                Assert.NotNull(reservation.AssignedDeskId);
            }
        }

        [Fact]
        public async Task AssignAsync_ShouldAccountForDeskPreference()
        {
            var context = GetInMemoryContext();

            var options = new SeedOptions();
            DbSeeder.Seed(context, options);

            var deskAssigner = new DeskAssigner(context);
            var date = options.ReservationDate;

            var failedTeams = await deskAssigner.AssignAsync(date);

            var reservations = context.Reservations
                .Include(r => r.assignedDesk)
                .Where(r => r.Date == date)
                .ToList();

            Assert.All(reservations, r => Assert.NotNull(r.AssignedDeskId));

            Assert.Empty(failedTeams);

            var reservationsDeskPreferenceSatisfied = reservations
                .Where(r => r.assignedDesk.DeskType == r.DeskTypePref)
                .Count();

            var reservationsDeskPreferenceNotSatisfied = reservations
                .Where(r => r.assignedDesk.DeskType != r.DeskTypePref)
                .Count();

            _output.WriteLine($"    Preferation satisfied: {reservationsDeskPreferenceSatisfied}");
            _output.WriteLine($"Preferation not satisfied: {reservationsDeskPreferenceNotSatisfied}");
        }
    }
}
