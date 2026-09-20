namespace TurfBooking.API.Models.Domain;

public class TurfImage
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public Turf Turf { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = string.Empty;
}