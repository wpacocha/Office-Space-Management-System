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
                .Where(t => prefix == null || t.name.StartsWith(prefix))
                .Select(t => t.name)
                .OrderBy(n => n)
                .Take(20)
                .ToList();

            return Ok(teams);
        }
    }
}
