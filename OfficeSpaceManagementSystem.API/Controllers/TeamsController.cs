using Microsoft.AspNetCore.Mvc;
using OfficeSpaceManagementSystem.API.Data;

namespace OfficeSpaceManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetTeams([FromQuery] string? prefix)
        {
            var teams = _context.Teams
                .AsEnumerable() // ⬅️ to przenosi resztę do pamięci (rozwiązuje problem z EF Core)
                .Where(t => string.IsNullOrEmpty(prefix) || t.name.ToLower().StartsWith(prefix.ToLower()))
                .Select(t => new { name = t.name })
                .OrderBy(n => n.name)
                .Take(20)
                .ToList();

            return Ok(teams);
        }
    }
}
