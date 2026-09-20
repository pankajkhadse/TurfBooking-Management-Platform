using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class TurfScheduleConfiguration
    : IEntityTypeConfiguration<TurfSchedule>
{
    public void Configure(EntityTypeBuilder<TurfSchedule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.IsAvailable)
            .IsRequired();

        builder.HasOne(x => x.Turf)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.TurfId,
            x.DayOfWeek
        })
        .IsUnique();
    }
}