using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(x => x.PhoneNumber)
               .HasMaxLength(15)
               .IsRequired();

        builder.HasIndex(x => x.Email)
               .IsUnique();

        builder.HasIndex(x => x.PhoneNumber)
               .IsUnique();
    }
}