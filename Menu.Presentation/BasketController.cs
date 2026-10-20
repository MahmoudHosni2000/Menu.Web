using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Baskets;
using Microsoft.AspNetCore.Mvc;
using Menu.Shared.Dtos.Baskets;
using Microsoft.AspNetCore.Http;
using Menu.Shared.Dtos.Errors;
using Microsoft.AspNetCore.Authorization;
using Menu.Shared.Constants;

namespace Menu.Presentation
{
    [Authorize(Roles = Roles.User)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BasketController(
        IServiceManager serviceManager) : ControllerBase
    {
        private readonly IBasketService _basketService =
            serviceManager.BasketService;

        [HttpGet("{basketId}")]
        [ProducesResponseType(
            typeof(BasketDto),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<BasketDto>> GetBasket(
            string basketId)
        {
            var basket =
                await _basketService.GetBasketAsync(basketId);

            return Ok(basket ?? new BasketDto
            {
                Id = basketId
            });
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(BasketDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ValidationErrorResponse),
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BasketDto>> UpdateBasket(
            BasketDto basket)
        {
            var updatedBasket =
                await _basketService.UpdateBasketAsync(basket);

            return updatedBasket is null
                ? BadRequest()
                : Ok(updatedBasket);
        }

        [HttpDelete("{basketId}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(ErrorResponse),
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBasket(
            string basketId)
        {
            var isDeleted =
                await _basketService.DeleteBasketAsync(basketId);

            return isDeleted
                ? NoContent()
                : NotFound(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "The basket was not found."
                });
        }
    }
}