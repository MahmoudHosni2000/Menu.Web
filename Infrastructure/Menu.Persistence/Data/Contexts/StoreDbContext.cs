using Menu.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Menu.Persistence.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Menu.Domain.Entities.Orders;

namespace Menu.Persistence.Data.Contexts
{
    public class StoreDbContext : IdentityDbContext<ApplicationUser>
    {
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
        {

        }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBrand> ProductBrands { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DeliveryMethod> DeliveryMethods { get; set; }
        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Apply configurations for Product, ProductBrand, and ProductType
            //modelBuilder.ApplyConfiguration(new Menu.Persistence.Data.Config.ProductsConfig());
            //modelBuilder.ApplyConfiguration(new Menu.Persistence.Data.Config.ProductBrandsConfig());
            //modelBuilder.ApplyConfiguration(new Menu.Persistence.Data.Config.ProductTypeConfig());

            // Alternatively, you can use the following line to apply all configurations from the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly); // Apply all configurations from the assembly
        }
    }
}
