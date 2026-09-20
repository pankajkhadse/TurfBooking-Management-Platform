using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class TurfSportConfiguration : IEntityTypeConfiguration<TurfSport>
{
    public void Configure(EntityTypeBuilder<TurfSport> builder)
    {
        builder.Property(x => x.SportType)
               .IsRequired();

        builder.HasOne(x => x.Turf)
               .WithMany(x => x.TurfSports)
               .HasForeignKey(x => x.TurfId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}