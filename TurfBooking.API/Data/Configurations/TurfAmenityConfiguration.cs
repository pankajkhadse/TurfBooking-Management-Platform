using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class TurfAmenityConfiguration : IEntityTypeConfiguration<TurfAmenity>
{
    public void Configure(EntityTypeBuilder<TurfAmenity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amenity)
            .IsRequired();

        builder.HasOne(x => x.Turf)
            .WithMany(x => x.TurfAmenities)
            .HasForeignKey(x => x.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.TurfId,
            x.Amenity
        })
        .IsUnique();
    }
}