using Menu.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Persistence.Data.Config
{
    public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.OwnsOne(item => item.Product, product =>
            {
                product.Property(details => details.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);
                product.Property(details => details.PictureUrl)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            builder.Property(item => item.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(item => item.Quantity)
                .IsRequired();
        }
    }
}
