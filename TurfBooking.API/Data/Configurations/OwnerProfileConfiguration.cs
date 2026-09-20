using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessName)
               .HasMaxLength(150)
               .IsRequired();

        builder.HasOne(x => x.ApplicationUser)
               .WithOne(x => x.OwnerProfile)
               .HasForeignKey<OwnerProfile>(x => x.ApplicationUserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}