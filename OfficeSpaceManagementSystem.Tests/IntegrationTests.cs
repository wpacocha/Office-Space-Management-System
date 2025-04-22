using Microsoft.EntityFrameworkCore;
using OfficeSpaceManagementSystem.API.Data;
using Xunit;

namespace OfficeSpaceManagementSystem.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void TestDatabaseSeeding()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            using (var context = new AppDbContext(options))
            {
                DbSeeder.Seed(context);

                Assert.True(context.Zones.Any());
                Assert.True(context.Teams.Any());
                Assert.True(context.Users.Any());
                Assert.True(context.Desks.Any());
                Assert.True(context.Reservations.Any());
            }
        }
    }
}
