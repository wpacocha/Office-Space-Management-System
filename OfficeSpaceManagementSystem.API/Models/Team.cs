using System.ComponentModel.DataAnnotations;

namespace OfficeSpaceManagementSystem.API.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string name { get; set; }

        public ICollection<User> Users { get; set; }
    }
}