using Microsoft.AspNetCore.Mvc;
using ChatSupportApi.Data;
using ChatSupportApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly ChatSupportDbContext _context;

        public TeamController(ChatSupportDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Agents)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    shift = t.Shift.ToString(),
                    isOverflow = t.IsOverflow,
                    capacity = t.CalculateCapacity(),
                    maxQueueSize = t.GetMaxQueueSize(),
                    agentCount = t.Agents.Count,
                    activeAgentCount = t.Agents.Count(a => a.IsActive)
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Agents)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = team.Id,
                name = team.Name,
                shift = team.Shift.ToString(),
                isOverflow = team.IsOverflow,
                capacity = team.CalculateCapacity(),
                maxQueueSize = team.GetMaxQueueSize(),
                agents = team.Agents.Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    seniority = a.Seniority.ToString(),
                    currentChatCount = a.CurrentChatCount,
                    maxConcurrency = a.GetMaxConcurrency(),
                    isActive = a.IsActive
                })
            });
        }
    }
}
