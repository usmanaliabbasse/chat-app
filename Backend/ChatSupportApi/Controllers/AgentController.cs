using Microsoft.AspNetCore.Mvc;
using ChatSupportApi.Data;
using ChatSupportApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly ChatSupportDbContext _context;
        private readonly ILogger<AgentController> _logger;

        public AgentController(ChatSupportDbContext context, ILogger<AgentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAgents()
        {
            var agents = await _context.Agents
                .Include(a => a.Team)
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    seniority = a.Seniority.ToString(),
                    teamName = a.Team!.Name,
                    currentChatCount = a.CurrentChatCount,
                    maxConcurrency = a.GetMaxConcurrency(),
                    isActive = a.IsActive,
                    isShiftEnding = a.IsShiftEnding
                })
                .ToListAsync();

            return Ok(agents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAgent(int id)
        {
            var agent = await _context.Agents
                .Include(a => a.Team)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = agent.Id,
                name = agent.Name,
                seniority = agent.Seniority.ToString(),
                teamName = agent.Team?.Name,
                currentChatCount = agent.CurrentChatCount,
                maxConcurrency = agent.GetMaxConcurrency(),
                isActive = agent.IsActive,
                isShiftEnding = agent.IsShiftEnding
            });
        }

        [HttpPut("{id}/shift-ending")]
        public async Task<IActionResult> SetShiftEnding(int id, [FromBody] bool isEnding)
        {
            var agent = await _context.Agents.FindAsync(id);

            if (agent == null)
            {
                return NotFound();
            }

            agent.IsShiftEnding = isEnding;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Agent {AgentId} shift ending set to {IsEnding}", id, isEnding);

            return Ok(new { message = "Agent shift status updated" });
        }

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetTeamAgents(int teamId)
        {
            var agents = await _context.Agents
                .Where(a => a.TeamId == teamId)
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    seniority = a.Seniority.ToString(),
                    currentChatCount = a.CurrentChatCount,
                    maxConcurrency = a.GetMaxConcurrency(),
                    isActive = a.IsActive
                })
                .ToListAsync();

            return Ok(agents);
        }
    }
}
