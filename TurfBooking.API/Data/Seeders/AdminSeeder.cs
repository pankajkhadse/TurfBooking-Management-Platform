using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Settings;

namespace TurfBooking.API.Data.Seeders;

public class AdminSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminSettings _settings;

    public AdminSeeder(
        UserManager<ApplicationUser> userManager,
        IOptions<AdminSettings> options)
    {
        _userManager = userManager;
        _settings = options.Value;
    }

    public async Task SeedAsync()
    {
        var admin = await _userManager.FindByEmailAsync(_settings.Email);

        if (admin != null)
            return;

        admin = new ApplicationUser
        {
            FullName = _settings.FullName,
            UserName = _settings.Email,
            Email = _settings.Email,
            PhoneNumber = _settings.PhoneNumber,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };

        var result = await _userManager.CreateAsync(admin, _settings.Password);

        if (!result.Succeeded)
            return;

        await _userManager.AddToRoleAsync(admin, "Admin");
    }
}