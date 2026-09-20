using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class BookingSlotConfiguration
    : IEntityTypeConfiguration<BookingSlot>
{
    public void Configure(EntityTypeBuilder<BookingSlot> builder)
    {
        builder.HasKey(x => x.Id);

        // One booking can contain multiple slots.
        builder.HasOne(x => x.Booking)
            .WithMany(x => x.BookingSlots)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Each slot belongs to one turf.
        builder.HasOne(x => x.Turf)
            .WithMany(x => x.BookingSlots)
            .HasForeignKey(x => x.TurfId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.BookingDate)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Critical double-booking protection.
        //
        // Two active reservations cannot occupy
        // the same turf + date + starting slot.
        builder.HasIndex(x => new
        {
            x.TurfId,
            x.BookingDate,
            x.StartTime
        })
        .IsUnique()
        .HasFilter("[IsActive] = 1");
    }
}