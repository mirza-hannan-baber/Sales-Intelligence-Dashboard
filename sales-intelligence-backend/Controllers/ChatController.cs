using Microsoft.AspNetCore.Mvc;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Services;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> PostChat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Chat message cannot be empty." });
            }

            try
            {
                var response = await _chatService.ProcessMessageAsync(request);
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new ChatResponse
                {
                    Answer = "Unable to connect to the backend service. Please make sure the backend is running.",
                    Intent = "error",
                    SuggestedQuestions = new List<string>
                    {
                        "How many sales agents are in our data?",
                        "How much revenue did employees generate in 2025?"
                    }
                });
            }
        }
    }
}
