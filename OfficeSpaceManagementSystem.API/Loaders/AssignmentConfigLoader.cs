using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OfficeSpaceManagementSystem.API.Loaders
{
    public class AssignmentConfigLoader
    {
        public class AssignmentConfig
        {
            public Dictionary<string, List<string>> SpecialTeams { get; set; }
            public List<TeamSizeRule> TeamSizeRules { get; set; }
        }

        public class TeamSizeRule
        {
            public int MinSize { get; set; }
            public int MaxSize { get; set; }
            public List<string> PriorityTypes { get; set; }
        }

        public static AssignmentConfig Load(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var config = JsonSerializer.Deserialize<AssignmentConfig>(json, options);

            if (config == null)
                throw new Exception("Invalid or empty assignment_config.json");

            return config;
        }
    }
}
