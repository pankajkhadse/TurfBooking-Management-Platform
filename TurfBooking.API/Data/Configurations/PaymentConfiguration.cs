using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RazorpayOrderId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RazorpayPaymentId)
            .HasMaxLength(100);

        builder.Property(x => x.RazorpaySignature)
            .HasMaxLength(200);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasIndex(x => x.RazorpayOrderId)
            .IsUnique();

        builder.HasOne(x => x.Booking)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BookingId)
            .IsUnique();
    }
}