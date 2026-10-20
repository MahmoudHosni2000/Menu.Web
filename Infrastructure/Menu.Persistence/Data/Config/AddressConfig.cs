using Menu.Persistence.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Persistence.Data.Config;

public class AddressConfig
    : IEntityTypeConfiguration<Address>
{
    public void Configure(
        EntityTypeBuilder<Address> builder)
    {
        builder.Property(address => address.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.Street)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(address => address.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(address => address.ApplicationUser)
            .WithOne(user => user.Address)
            .HasForeignKey<Address>(
                address => address.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}