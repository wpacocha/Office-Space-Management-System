using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfficeSpaceManagementSystem.API.Data;

namespace OfficeSpaceManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunAssignment([FromQuery] DateOnly date)
        {
            var assigner = new DeskAssigner(_context);
            var failed = await assigner.AssignAsync(date);

            return Ok(new
            {
                Message = "Desk assignment completed.",
                FailedAssignments = failed
            });
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateAssignment([FromQuery] DateOnly date)
        {
            var validator = new DeskAssignmentValidator(_context);
            var (success, failedTeams) = await validator.ValidateAsync(date);

            return Ok(new
            {
                Success = success,
                FailedTeams = failedTeams
            });
        }

    }
}
