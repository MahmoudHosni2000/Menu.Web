using Microsoft.EntityFrameworkCore;
using Menu.Persistence.Data.Contexts;
using Menu.Domain.Contracts;
using Menu.Persistence;
using Menu.Web.Middlewares;
using Menu.Services.Mapping.Products;
using Menu.Services;
using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Products;
using Menu.Services.Products;
using Menu.Shared.Dtos.Errors;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Menu.Persistence.Repos;
using Menu.Services.Mapping.Baskets;
using Menu.Services.Abstraction.Baskets;
using Menu.Services.Baskets;
using Menu.Services.Abstraction.Cache;
using Menu.Services.Cache;
using Menu.Persistence.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Menu.Persistence.Identity;
using Menu.Services.Abstraction.Identity;
using Menu.Shared.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;
using Menu.Services.Mapping.Orders;
using Menu.Services.Abstraction.Orders;
using Menu.Services.Orders;
using Menu.Services.Abstraction.Payments;
using Menu.Persistence.Payments;

namespace Menu.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage)
                        .ToArray();

                    var response = new ValidationErrorResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Validation failed.",
                        Errors = errors
                    };

                    return new BadRequestObjectResult(response);
                };
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });
                options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document,
                        null)] = new List<string>()
                });
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<StoreDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<StoreDbContext>();

            builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection("Jwt"));

            var jwtOptions = builder.Configuration
            .GetSection("Jwt")
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are not configured.");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // here we are setting the parameters for validating the JWT token, including issuer, audience, lifetime, and signing key. This ensures that the token is valid and has not been tampered with.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Set the valid issuer and audience from the JWT options, and create a symmetric security key using the configured key for signing the token.
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key))
                };
            });

            // Configure Stripe settings from the configuration file and bind them to the StripeOptions class. This allows the application to access Stripe-related settings, such as API keys and other configuration values, for processing payments.
            builder.Services.Configure<StripeOptions>(
            builder.Configuration.GetSection(
                StripeOptions.SectionName));

            var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") ?? throw new InvalidOperationException("Redis connection string is not configured.");
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

            builder.Services.AddScoped<IDbInitializer, DbInitializer>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IServiceManager, ServiceManager>();
            builder.Services.AddScoped<IBasketRepository, BasketRepository>();
            builder.Services.AddScoped<IBasketService, BasketService>();
            builder.Services.AddScoped<ICacheRepository, CacheRepository>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            // Register the AutoMapper service to map between domain models and DTOs by adding the ProductProfile mapping configuration. This service will be used to automatically map properties between different object types, reducing the need for manual mapping code.
            builder.Services.AddAutoMapper(config =>
            {
                config.AddProfile<ProductProfile>();
                config.AddProfile<BasketProfile>();
                config.AddProfile<OrderProfile>();
            });

            // Register the IHttpContextAccessor service to access the current HTTP context in services and other components. This service will be used to retrieve information about the current HTTP request, such as headers, user identity, and other request-specific data.
            builder.Services.AddHttpContextAccessor();

            // Register the ProductPicUrlResolver service to resolve product picture URLs by implementing the IValueResolver interface. This service will be used to transform product picture paths into fully qualified URLs when mapping between domain models and DTOs.
            builder.Services.AddTransient<ProductPicUrlResolver>();

            var app = builder.Build();

            // Add the GlobalExceptionHandlingMiddleware to the middleware pipeline to handle exceptions globally. This middleware will catch unhandled exceptions, log them, and return a standardized error response to the client.
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            // Add a custom status code page middleware to handle HTTP status codes and return a custom error message. This middleware will intercept responses with specific status codes and provide a user-friendly error message instead of the default response.
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var response = statusCodeContext.HttpContext.Response;
                if (response.StatusCode == StatusCodes.Status404NotFound)
                {
                    // Create an ErrorResponse object with a 404 status code and a custom message indicating that the requested resource was not found. This object will be serialized to JSON and sent back to the client as the response body.
                    var errorResponse = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "The requested resource was not found."
                    };
                    await response.WriteAsJsonAsync(errorResponse);
                }
            });

            // Create a scope to resolve the IDbInitializer service and call the InitializeAsync method to seed the database with initial data.
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                await initializer.InitializeAsync();
            }


            // Configure the HTTP request pipeline.
            var enableSwagger =
                app.Environment.IsDevelopment()
                || app.Configuration.GetValue<bool>("EnableSwagger");

            if (enableSwagger)
            {
                app.MapOpenApi();

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
