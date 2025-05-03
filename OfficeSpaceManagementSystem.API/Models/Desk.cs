using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeSpaceManagementSystem.API.Models
{
	public class Desk
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey("Zone")]
		public int ZoneId { get; set; }

		public Zone Zone { get; set; }

		[Required]
		public DeskType DeskType { get; set; }

		[Required]
		public string Name { get; set; }

		public ICollection<Reservation> Reservations { get; set; }
	}
	public enum DeskType
	{
		WideMonitor,
		DualMonitor
	}
}

