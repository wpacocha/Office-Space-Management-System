using System;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.DTOs
{
    public class CreateReservationDto
    {
        public DateOnly Date { get; set; }
        public DeskType DeskTypePref { get; set; }
        public bool IsFocusMode { get; set; }
        public string? TeamName { get; set; }
        public int UserId { get; set; }
    }
}
