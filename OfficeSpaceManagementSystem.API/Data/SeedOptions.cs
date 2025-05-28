using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class SeedOptions
    {
        public int TotalUsers { get; set; } = 900;
        public int TotalTeams { get; set; } = 150;
        public int MinUsersPerTeam { get; set; } = 1;
        public int MaxUsersPerTeam { get; set; } = 15;

        public int ReservationsCount { get; set; } = 200;
        public DateOnly ReservationDate { get; set; } = new DateOnly(2025, 5, 28);

        public Func<int, DeskType>? DeskTypeSelector { get; set; } = null;
    }
}
