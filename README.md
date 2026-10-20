# Menu.Web

ASP.NET Core 10 API for a menu and ordering application, using SQL Server, Entity Framework Core, Redis, JWT authentication, and Stripe payments.

## Hosted API

[Open Swagger UI](https://restaurantmenuapi.runasp.net/swagger/index.html)

Base URL: https://restaurantmenuapi.runasp.net

## Structure

- `Core/Menu.Domain`: entities and repository contracts.
- `Infrastructure/Menu.Persistence`: EF Core, migrations, repositories, identity, and payments.
- `Menu.Services` and `Menu.Services.Abstraction`: application services and contracts.
- `Menu.Shared`: DTOs and settings.
- `Menu.Presentation`: API controllers.
- `Menu.Web`: application startup and configuration.

## Run locally

Install the .NET 10 SDK and run SQL Server and Redis. Configure the following keys using .NET User Secrets or environment variables before starting the API:

- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:RedisConnection`
- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience`
- Stripe settings defined in `Menu.Shared/Settings/StripeOptions.cs` when using payments.

Environment variables use double underscores instead of colons, for example `ConnectionStrings__DefaultConnection`. Keep real credentials outside source control.

```powershell
dotnet restore Menu.Web.slnx
dotnet build Menu.Web.slnx
dotnet run --project Menu.Web
```

Startup initializes the database. Swagger is available at `/swagger` in the Development environment once the application starts successfully.
