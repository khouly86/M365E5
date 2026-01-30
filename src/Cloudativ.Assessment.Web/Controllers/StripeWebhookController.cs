using Cloudativ.Assessment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cloudativ.Assessment.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ISubscriptionService subscriptionService,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest("Missing Stripe signature");
        }

        try
        {
            await _subscriptionService.HandleStripeWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest($"Webhook error: {ex.Message}");
        }
    }
}
