using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeSpaceManagementSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFocusModeToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ZonePreference",
                table: "Reservations",
                newName: "isFocusMode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isFocusMode",
                table: "Reservations",
                newName: "ZonePreference");
        }
    }
}
