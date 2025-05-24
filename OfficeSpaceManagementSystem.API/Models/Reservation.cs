using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeSpaceManagementSystem.API.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DeskType DeskTypePref { get; set; }

        [Required]
        public bool isFocusMode { get; set; }

        [ForeignKey("Desk")]
        public int? AssignedDeskId { get; set; }
        public Desk? assignedDesk { get; set; }
    }
}
