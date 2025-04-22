using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class SeedOptions
    {
        public int TotalUsers { get; set; } = 300;
        public int TotalTeams { get; set; } = 100;
        public int MinUsersPerTeam { get; set; } = 1;
        public int MaxUsersPerTeam { get; set; } = 40;

        public int ReservationsCount { get; set; } = 200;
        public DateOnly ReservationDate { get; set; } = new DateOnly(2024, 4, 25);

        public Func<int, DeskType>? DeskTypeSelector { get; set; } = null;
        public Func<int, int>? ZonePreferenceSelector { get; set; } = null;
    }
}
