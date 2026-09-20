using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using TurfBooking.API.Services.Interfaces;
using TurfBooking.API.Settings;

namespace TurfBooking.API.Services.Implementations;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);

        _cloudinary = new Cloudinary(account);

        _cloudinary.Api.Secure = true;
    }

    public async Task<(string ImageUrl, string PublicId)> UploadAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "turf-booking/turfs"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception(result.Error.Message);

        return (
            result.SecureUrl.ToString(),
            result.PublicId
        );
    }

    public async Task DeleteAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);

        await _cloudinary.DestroyAsync(deleteParams);
    }
}