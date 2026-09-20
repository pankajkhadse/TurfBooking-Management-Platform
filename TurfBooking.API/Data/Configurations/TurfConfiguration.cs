using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class TurfConfiguration : IEntityTypeConfiguration<Turf>
{
    public void Configure(EntityTypeBuilder<Turf> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(x => x.Address)
               .HasMaxLength(300);

        builder.Property(x => x.City)
               .HasMaxLength(100);

        builder.Property(x => x.State)
               .HasMaxLength(100);

        builder.Property(x => x.PricePerSlot)
               .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Latitude)
               .HasColumnType("decimal(9,6)");

        builder.Property(x => x.Longitude)
               .HasColumnType("decimal(9,6)");

        builder.HasOne(x => x.OwnerProfile)
               .WithMany(x => x.Turfs)
               .HasForeignKey(x => x.OwnerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}