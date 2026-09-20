
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Configurations;

public class WebhookEventConfiguration
    : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RazorpayEventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(x => x.RazorpayEventId)
            .IsUnique();

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.IsProcessed)
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .IsRequired();

        builder.Property(x => x.ProcessingError)
            .HasColumnType("nvarchar(max)");
    }
}
