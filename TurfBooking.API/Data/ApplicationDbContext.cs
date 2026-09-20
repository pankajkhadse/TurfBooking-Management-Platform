using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<OwnerProfile> OwnerProfiles => Set<OwnerProfile>();

    public DbSet<Turf> Turfs => Set<Turf>();

    public DbSet<Sport> Sports => Set<Sport>();

    public DbSet<TurfSport> TurfSports => Set<TurfSport>();

    public DbSet<TurfImage> TurfImages => Set<TurfImage>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Amenity> Amenities => Set<Amenity>();

    public DbSet<TurfAmenity> TurfAmenities => Set<TurfAmenity>();

    public DbSet<TurfSchedule> TurfSchedules => Set<TurfSchedule>();

    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}