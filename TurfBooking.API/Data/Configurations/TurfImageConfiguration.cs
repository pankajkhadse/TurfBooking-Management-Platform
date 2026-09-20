using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class TurfImageConfiguration : IEntityTypeConfiguration<TurfImage>
{
    public void Configure(EntityTypeBuilder<TurfImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImageUrl)
               .IsRequired();

        builder.Property(x => x.PublicId)
               .IsRequired();

        builder.HasOne(x => x.Turf)
               .WithMany(x => x.TurfImages)
               .HasForeignKey(x => x.TurfId);
    }
}