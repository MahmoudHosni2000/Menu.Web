using Menu.Shared.Constants;
using Menu.Domain.Contracts;
using Menu.Domain.Entities.Products;
using Menu.Persistence.Data.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Menu.Domain.Entities.Orders;

namespace Menu.Persistence
{
    // This class is responsible for initializing the database, applying migrations, and seeding initial data.
    public class DbInitializer : IDbInitializer
    {
        private readonly StoreDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(
            StoreDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.MigrateAsync();

            // Seed Roles
            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole(Roles.Admin));
            }

            if (!await _roleManager.RoleExistsAsync(Roles.User))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole(Roles.User));
            }
            //

            var seedDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "DataSeeding");

            if (!await _context.ProductBrands.AnyAsync())
            {
                var jsonData = await File.ReadAllTextAsync(Path.Combine(seedDirectory, "brands.json"));
                var productBrands = JsonSerializer.Deserialize<List<ProductBrand>>(jsonData);

                if (productBrands is not null)
                {
                    await _context.ProductBrands.AddRangeAsync(productBrands);
                    await _context.SaveChangesAsync();
                }
            }

            if (!await _context.ProductTypes.AnyAsync())
            {
                var jsonData = await File.ReadAllTextAsync(Path.Combine(seedDirectory, "types.json"));
                var productTypes = JsonSerializer.Deserialize<List<ProductType>>(jsonData);

                if (productTypes is not null)
                {
                    await _context.ProductTypes.AddRangeAsync(productTypes);
                    await _context.SaveChangesAsync();
                }
            }

            if (!await _context.Products.AnyAsync())
            {
                var jsonData = await File.ReadAllTextAsync(Path.Combine(seedDirectory, "products.json"));
                var products = JsonSerializer.Deserialize<List<Product>>(jsonData);

                if (products is not null)
                {
                    await _context.Products.AddRangeAsync(products);
                    await _context.SaveChangesAsync();
                }
            }

            if (!await _context.DeliveryMethods.AnyAsync())
            {
                var jsonData = await File.ReadAllTextAsync(
                    Path.Combine(seedDirectory, "delivery.json"));

                var deliveryMethods =
                    JsonSerializer.Deserialize<List<DeliveryMethod>>(jsonData);

                if (deliveryMethods is not null)
                {
                    await _context.DeliveryMethods.AddRangeAsync(deliveryMethods);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
