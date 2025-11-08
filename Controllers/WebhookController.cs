using Microsoft.AspNetCore.Mvc;
using location_img_poc.Models;
using location_img_poc.Services;
using System.Text.Json;

namespace location_img_poc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly WhatsAppService _service;
        private const string VERIFY_TOKEN = "MySecretToken"; // must match in Meta setup

        public WebhookController(WhatsAppService service)
        {
            _service = service;
        }

        // ✅ Step 1: Webhook Verification
        [HttpGet]
        public IActionResult Verify(
            [FromQuery] string hub_mode,
            [FromQuery] string hub_challenge,
            [FromQuery] string hub_verify_token)
        {
            if (hub_mode == "subscribe" && hub_verify_token == VERIFY_TOKEN)
                return Ok(hub_challenge);
            return Forbid();
        }

        // ✅ Step 2: Handle Incoming Messages
        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] JsonElement data)
        {
            try
            {
                Console.WriteLine("📩 Incoming Message:");
                Console.WriteLine(data.ToString());

                var message = _service.ParseMessage(data);
                if (message != null)
                {
                    await _service.SaveToDatabase(message);

                    // Auto reply to sender
                    await _service.SendReply(message.From!, "✅ Received your message! Thanks for sharing.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Webhook error: {ex.Message}");
                return BadRequest();
            }
        }
    }
}
