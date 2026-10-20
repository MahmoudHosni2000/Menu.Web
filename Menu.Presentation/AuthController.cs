using Menu.Services.Abstraction.Identity;
using Microsoft.AspNetCore.Mvc;
using Menu.Shared.Dtos.Identity;
using Menu.Shared.Dtos.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Menu.Presentation
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController(
        IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService =
            authService;

        [HttpPost("register")]
        [ProducesResponseType(
            typeof(UserDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ValidationErrorResponse),
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Register(
            UserRegisterDto registerDto)
        {
            var user = await _authService.RegisterAsync(registerDto);

            return Ok(user);
        }

        [HttpPost("login")]
        [ProducesResponseType(
            typeof(UserDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ErrorResponse),
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Login(
            UserLoginDto loginDto)
        {
            var user = await _authService.LoginAsync(loginDto);

            return Ok(user);
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status200OK)]
        [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>>
        GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var user = await _authService
                .GetCurrentUserAsync(email);

            return user is null
                ? Unauthorized()
                : Ok(user);
        }

        [HttpGet("email-exists")]
        [ProducesResponseType(
            typeof(bool),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>>
            CheckEmailExists([FromQuery] string email)
        {
            var exists = await _authService
                .CheckEmailExistsAsync(email);

            return Ok(exists);
        }

        [Authorize]
        [HttpGet("address")]
        [ProducesResponseType(
        typeof(AddressDto),
        StatusCodes.Status200OK)]
        [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AddressDto>>
        GetCurrentUserAddress()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var address = await _authService
                .GetCurrentUserAddressAsync(email);

            if (address is null)
            {
                return NotFound(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "The address was not found."
                });
            }

            return Ok(address);
        }

        [Authorize]
        [HttpPut("address")]
        [ProducesResponseType(
            typeof(AddressDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ErrorResponse),
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AddressDto>>
            UpdateCurrentUserAddress(AddressDto addressDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var address = await _authService
                .UpdateCurrentUserAddressAsync(
                    email,
                    addressDto);

            if (address is null)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "The address could not be updated."
                });
            }

            return Ok(address);
        }
    }
}