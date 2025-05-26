using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Data
{
    public class SeedOptions
    {
        public int TotalUsers { get; set; } = 900;
        public int TotalTeams { get; set; } = 150;
        public int MinUsersPerTeam { get; set; } = 1;
        public int MaxUsersPerTeam { get; set; } = 15;

        public int ReservationsCount { get; set; } = null;
        public DateOnly ReservationDate { get; set; } = null;

        public Func<int, DeskType>? DeskTypeSelector { get; set; } = null;
        public Func<int, int>? ZonePreferenceSelector { get; set; } = null;
    }
}
