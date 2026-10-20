using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Orders;
using Menu.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Menu.Shared.Dtos.Orders;
using Menu.Shared.Dtos.Errors;
using System.Security.Claims;

namespace Menu.Presentation;

[Authorize(Roles = Roles.User)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController(
    IServiceManager serviceManager) : ControllerBase
{
    private readonly IOrderService _orderService =
        serviceManager.OrderService;

    [HttpPost]
    [ProducesResponseType(
    typeof(OrderResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ErrorResponse),
    StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
    OrderRequest request)
    {
        var userEmail =
            User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(userEmail))
            return Unauthorized();

        var order = await _orderService.CreateOrderAsync(
            request,
            userEmail);

        if (order is null)
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "The order could not be created."
            });
        }

        return Ok(order);
    }

    [HttpGet("delivery-methods")]
    [ProducesResponseType(
    typeof(IEnumerable<DeliveryMethodResponse>),
    StatusCodes.Status200OK)]
    public async Task<
    ActionResult<IEnumerable<DeliveryMethodResponse>>>
    GetAllDeliveryMethods()
    {
        var deliveryMethods =
            await _orderService.GetAllDeliveryMethodsAsync();

        return Ok(deliveryMethods);
    }

    [HttpGet]
    [ProducesResponseType(
    typeof(IEnumerable<OrderResponse>),
    StatusCodes.Status200OK)]
    public async Task<
    ActionResult<IEnumerable<OrderResponse>>>
    GetOrdersForCurrentUser()
    {
        var userEmail =
            User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(userEmail))
            return Unauthorized();

        var orders = await _orderService
            .GetOrdersForSpecificUserAsync(userEmail);

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>>
        GetOrderById(int id)
    {
        var userEmail =
            User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(userEmail))
            return Unauthorized();

        var order = await _orderService
            .GetOrderByIdForSpecificUserAsync(
                id,
                userEmail);

        if (order is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "The order was not found."
            });
        }

        return Ok(order);
    }
}

