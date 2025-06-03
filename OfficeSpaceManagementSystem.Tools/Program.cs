using OfficeSpaceManagementSystem.Tools;
using OfficeSpaceManagementSystem.Tools.Exporters;
using OfficeSpaceManagementSystem.API.Data;

await DeskAssignmentExporter.RunAsync(
    iterations: 100,
    csvPath: "desk_assignment_stats_average.csv",
    optionsFactory: i => new SeedOptions());

//await DeskAssignmentExporter.RunAsync(
//    iterations: 100,
//    csvPath: "desk_assignment_stats_average_full.csv",
//    optionsFactory: i => new SeedOptions { 
//        ReservationsCount = 223 
//    });