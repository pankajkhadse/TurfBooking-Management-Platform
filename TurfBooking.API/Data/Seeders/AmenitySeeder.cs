using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Data.Seeders;

public static class AmenitySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (context.Amenities.Any())
            return;

        var amenities = new List<Amenity>
        {
            new Amenity { Name = "Parking" },
            new Amenity { Name = "Changing Room" },
            new Amenity { Name = "Washroom" },
            new Amenity { Name = "Drinking Water" },
            new Amenity { Name = "Flood Lights" },
            new Amenity { Name = "Seating Area" },
            new Amenity { Name = "First Aid" },
            new Amenity { Name = "Cafeteria" }
        };

        await context.Amenities.AddRangeAsync(amenities);
        await context.SaveChangesAsync();
    }
}