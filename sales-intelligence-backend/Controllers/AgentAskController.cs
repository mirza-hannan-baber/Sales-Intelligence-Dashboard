using Microsoft.AspNetCore.Mvc;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Services;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/agent/ask")]
    public class AgentAskController : ControllerBase
    {
        private readonly IGroqAgentService _groqAgentService;
        private readonly ILogger<AgentAskController> _logger;

        public AgentAskController(IGroqAgentService groqAgentService, ILogger<AgentAskController> logger)
        {
            _groqAgentService = groqAgentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AgentAskRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new AgentAskResponse
                {
                    Answer = "Question cannot be empty.",
                    SqlUsed = string.Empty
                });
            }

            try
            {
                var response = await _groqAgentService.AskAsync(request.Question, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing /api/agent/ask request");
                return StatusCode(500, new AgentAskResponse
                {
                    Answer = "An error occurred while processing your question: " + ex.Message,
                    SqlUsed = string.Empty
                });
            }
        }
    }
}
