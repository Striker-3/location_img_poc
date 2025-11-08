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
    [FromQuery(Name = "hub.mode")] string? mode,
    [FromQuery(Name = "hub.challenge")] string? challenge,
    [FromQuery(Name = "hub.verify_token")] string? token)
{
    Console.WriteLine($"👉 Webhook verification request: mode={mode}, token={token}, challenge={challenge}");

    if (mode == "subscribe" && token == VERIFY_TOKEN)
    {
        Console.WriteLine("✅ Webhook verified successfully.");
        return Ok(challenge);
    }

    Console.WriteLine("❌ Webhook verification failed.");
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
