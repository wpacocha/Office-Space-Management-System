using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OfficeSpaceManagementSystem.API.Models;

namespace OfficeSpaceManagementSystem.API.Loaders
{
    public class ZoneConfigLoader
    {
        public class ZoneConfig
        {
            public List<ZoneEntry> Zones { get; set; }
        }

        public class ZoneEntry
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int Floor { get; set; }
            public int TotalDesks { get; set; }
            public int WideMonitorDesks { get; set; }
            public int DualMonitorDesks { get; set; }
            public string FirstDeskType { get; set; }
        }

        public static List<Zone> LoadZones(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize<ZoneConfig>(json);

            if (config?.Zones == null)
                throw new Exception("Invalid or empty zone_config.json");

            var zones = new List<Zone>();

            foreach (var entry in config.Zones)
            {
                zones.Add(new Zone
                {
                    Name = entry.Name,
                    Type = Enum.Parse<ZoneType>(entry.Type),
                    Florr = entry.Floor,
                    TotalDesks = entry.TotalDesks,
                    WideMonitorDesks = entry.WideMonitorDesks,
                    DualMonitorDesks = entry.DualMonitorDesks,
                    FirstDeskType = Enum.Parse<DeskType>(entry.FirstDeskType)
                });
            }

            return zones;
        }
    }
}
