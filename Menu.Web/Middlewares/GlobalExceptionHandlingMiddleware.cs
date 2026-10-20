using Menu.Shared.Dtos.Errors;
using Menu.Domain.Exceptions;

namespace Menu.Web.Middlewares
{
    // Middleware for global exception handling
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // InvokeAsync method to handle exceptions
        public async Task InvokeAsync(HttpContext context)
        {
            // Middleware logic goes here
            try
            {
                await _next(context);
            }
            catch(Exception exception) 
            {
                var statusCode = exception switch
                {
                    ProductNotFoundException =>
                        StatusCodes.Status404NotFound,

                    UserRegistrationException =>
                        StatusCodes.Status400BadRequest,

                    InvalidCredentialsException =>
                        StatusCodes.Status400BadRequest,

                    _ =>
                        StatusCodes.Status500InternalServerError
                };

                var message = exception switch
                {
                    ProductNotFoundException =>
                        exception.Message,

                    UserRegistrationException =>
                        "User registration failed.",

                    InvalidCredentialsException =>
                        exception.Message,

                    _ =>
                        "An unexpected error occurred."
                };

                context.Response.StatusCode = statusCode;

                context.Response.ContentType = "application/json";

                // Log the exception details
                if (exception is ProductNotFoundException)
                {
                    _logger.LogWarning(
                        "Product was not found: {Message}",
                        exception.Message);
                }
                else
                {
                    _logger.LogError(
                        exception,
                        "An unhandled exception occurred.");
                }

                if (exception is UserRegistrationException registrationException)
                {
                    var validationResponse = new ValidationErrorResponse
                    {
                        StatusCode = statusCode,
                        Message = message,
                        Errors = registrationException.Errors
                    };

                    await context.Response.WriteAsJsonAsync(validationResponse);
                    return;
                }

                // Create an error response object with status code and message
                var errorResponse = new ErrorResponse
                {
                    StatusCode = statusCode,
                    Message = message
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
