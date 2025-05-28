using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OfficeSpaceManagementSystem.API.Models
{
    public class Zone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public ZoneType Type { get; set; }

        [Required]
        [Range(0, 2)]
        public int Florr { get; set; }

        [Required]
        public int TotalDesks { get; set; }

        [JsonPropertyName("WideMonitorDesks")]
        public int WideMonitorDesks { get; set; }

        [JsonPropertyName("DualMonitorDesks")]
        public int DualMonitorDesks { get; set; }

        [Required]
        public DeskType FirstDeskType { get; set; }

        public ICollection<Desk> Desks { get; set; }

    }
    public enum ZoneType
    {
        Standard,
        Audio,
        HR,
        Executive,
        DuoFocus,
        Focus,
        WarRoom
    }
}