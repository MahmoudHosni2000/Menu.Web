using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Payments;
using Menu.Shared.Constants;
using Menu.Shared.Dtos.Baskets;
using Menu.Shared.Dtos.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Menu.Shared.Settings;
using Microsoft.Extensions.Options;
using Stripe;

namespace Menu.Presentation;

[Authorize(Roles = Roles.User)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController(
    IServiceManager serviceManager,
    IOptions<StripeOptions> stripeOptions) : ControllerBase
{
    private readonly IPaymentService _paymentService =
        serviceManager.PaymentService;

    private readonly string _webhookSecret =
        stripeOptions.Value.WebhookSecret;

    [HttpPost("{basketId}")]
    [ProducesResponseType(
        typeof(BasketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BasketDto>>
        CreateOrUpdatePaymentIntent(string basketId)
    {
        var basket = await _paymentService
            .CreateOrUpdatePaymentIntentAsync(basketId);

        if (basket is null)
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode =
                    StatusCodes.Status400BadRequest,
                Message =
                    "Payment intent could not be created."
            });
        }

        return Ok(basket);
    }

    [AllowAnonymous] // This endpoint is called by Stripe, so it should be accessible without authentication
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(
            Request.Body);

        var json = await reader.ReadToEndAsync();

        Stripe.Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"].ToString(),
                _webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            Console.WriteLine(
                $"Stripe Webhook Error: {exception.Message}");

            return BadRequest();
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded
            && stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            var updated = await _paymentService
                .UpdateOrderPaymentStatusAsync(paymentIntent.Id, true);

            if (!updated)
                return BadRequest();
        }
        else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed
            && stripeEvent.Data.Object is PaymentIntent failedPaymentIntent)
        {
            var updated = await _paymentService
                .UpdateOrderPaymentStatusAsync(failedPaymentIntent.Id, false);

            if (!updated)
                return BadRequest();
        }

        return Ok();
    }
}