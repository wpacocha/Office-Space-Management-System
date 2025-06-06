using OfficeSpaceManagementSystem.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=office.db"));

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

// Seeder
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var today = DateOnly.FromDateTime(DateTime.Today.AddDays(1)); // przykładowo na jutro
    var options = new SeedOptions
    {
        ReservationDate = today,
        ReservationsCount = 200,
        TotalTeams = 100,
        TotalUsers = 300,
        MinUsersPerTeam = 2,
        MaxUsersPerTeam = 30,
        FocusModePercentage = 0.15
    };

    DbSeeder.Seed(context, options);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/validate", async (AppDbContext db) =>
{
    var validator = new DeskAssignmentValidator(db);
    var result = await validator.ValidateAsync(DateOnly.FromDateTime(DateTime.Today));

    return result.Success
        ? Results.Ok("✅ Wszystkie zespoły można przypisać bez dzielenia.")
        : Results.BadRequest($"❌ Nie można przypisać zespołów: {string.Join(", ", result.FailedTeams)}");
});

app.MapPost("/assign", async (AppDbContext db) =>
{
    var assigner = new DeskAssigner(db);
    var failedTeams = await assigner.AssignAsync(DateOnly.FromDateTime(DateTime.Today));

    return failedTeams.Count == 0
        ? Results.Ok("✅ Biurka przypisane pomyślnie.")
        : Results.BadRequest($"❌ Nie udało się przypisać: {string.Join(", ", failedTeams)}");
});

app.Run();
