using Menu.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Persistence.Data.Config
{
    public class DeliveryMethodConfig
        : IEntityTypeConfiguration<DeliveryMethod>
    {
        public void Configure(
            EntityTypeBuilder<DeliveryMethod> builder)
        {
            builder.Property(method => method.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(method => method.ShortName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(method => method.Description)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(method => method.DeliveryTime)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}