using System.ComponentModel.DataAnnotations;

namespace OfficeSpaceManagementSystem.API.Models
{
    public class Zone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(1, 4)]
        public int Priority { get; set; }

        [Required]
        [Range(0, 2)]
        public int Florr { get; set; }

        [Required]
        public int StandardDesks { get; set; }

        [Required]
        public int DualMonitorDesks { get; set; }

        [Required]
        public int SuperchargedDesks { get; set; }

        [Required]
        public int TotalDesks { get; set; }

        public ICollection<Desk> Desks { get; set; }

    }
}