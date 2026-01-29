using Microsoft.AspNetCore.Mvc;
using ZavaStorefront.Models;
using ZavaStorefront.Services;

namespace ZavaStorefront.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly FoundryChatService _chatService;

        public ChatController(ILogger<ChatController> logger, FoundryChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required." });
            }

            try
            {
                var reply = await _chatService.SendAsync(request.Message, cancellationToken);
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat request failed");
                return StatusCode(500, new { error = "Unable to get a response right now." });
            }
        }
    }
}
