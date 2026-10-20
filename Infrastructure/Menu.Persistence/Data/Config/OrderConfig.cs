using Menu.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Persistence.Data.Config
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.Property(order => order.BuyerEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(order => order.SubTotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(order => order.Status)
                .HasConversion<string>();

            builder.OwnsOne(order => order.ShippingAddress, address =>
            {
                address.Property(a => a.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.LastName)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.Street)
                    .IsRequired()
                    .HasMaxLength(256);
                address.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.Country)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            builder.HasOne(order => order.DeliveryMethod)
                .WithMany()
                .HasForeignKey(order => order.DeliveryMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(order => order.Items)
                .WithOne()
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(order => order.PaymentIntentId)
                .IsRequired()
                .HasMaxLength(256);
        }
    }
}
