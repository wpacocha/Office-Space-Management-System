using OfficeSpaceManagementSystem.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=office.db"));

var app = builder.Build();

builder.Services.AddControllers();
app.MapControllers();

// Seeder
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db);
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
