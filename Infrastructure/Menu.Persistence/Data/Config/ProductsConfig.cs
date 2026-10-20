using Menu.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Persistence.Data.Config
{
    public class ProductsConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(p => p.Name)
                .HasColumnType("varchar")
                .HasMaxLength(256);
            builder.Property(p => p.Description)
                .HasColumnType("varchar")
                .HasMaxLength(512);
            builder.Property(p => p.ImageUrl)
                .HasColumnType("varchar")
                .HasMaxLength(256);
            builder.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(p => p.Type).WithMany().HasForeignKey(p => p.TypeId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
