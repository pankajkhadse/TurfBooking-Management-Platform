using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests.Turf;
using TurfBooking.API.Models.Responses.Turf;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Implementations
{
    public class TurfService : ITurfService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IBookingLifecycleService _bookingLifecycleService;

        private readonly IImageService _imageService;
        public TurfService(
         ApplicationDbContext context,
          ICurrentUserService currentUser, IImageService imageService,
          IBookingLifecycleService bookingLifecycleService)
        {
            _context = context;
            _currentUser = currentUser;
            _imageService = imageService;
            _bookingLifecycleService = bookingLifecycleService;
        }



        private async Task<OwnerProfile> GetCurrentOwnerAsync()
        {
            var owner = await _context.OwnerProfiles
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == _currentUser.UserId &&
                    x.Status == OwnerStatus.Approved);

            if (owner == null)
                throw new UnauthorizedException("Owner profile not found or not approved.");

            return owner;
        }
        public async Task<ApiResponse<TurfResponse>> CreateAsync(CreateTurfRequest request)
        {

           

            var owner = await _context.OwnerProfiles
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == _currentUser.UserId &&
                    x.Status == OwnerStatus.Approved);

            if (owner == null)
                throw new UnauthorizedException("Owner profile not found or not approved.");

            var exists = await _context.Turfs.AnyAsync(x =>
                x.OwnerProfileId == owner.Id &&
                 x.Name.ToLower() == request.Name.ToLower());

            if (exists)
            {
                throw new BadRequestException("You already have a turf with this name.");
            }

            if (request.SlotDurationMinutes <= 0)
            {
                throw new BadRequestException(
                    "Slot duration must be greater than 0 minutes.");
            }

            if (request.SlotDurationMinutes > 1440)
            {
                throw new BadRequestException(
                    "Slot duration cannot exceed 24 hours.");
            }

            var turf = new Models.Domain.Turf
            {
                OwnerProfileId = owner.Id,
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                Locality = request.Locality,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                PricePerSlot = request.PricePerSlot,
                SlotDurationMinutes = request.SlotDurationMinutes,
                Status = TurfStatus.Active
            };

            _context.Turfs.Add(turf);

            await _context.SaveChangesAsync();

            return ApiResponse<TurfResponse>.SuccessResponse(
                new TurfResponse
                {
                    Id = turf.Id,
                    Name = turf.Name,
                    Description = turf.Description,
                    Address = turf.Address,
                    Locality = turf.Locality,
                    City = turf.City,
                    State = turf.State,
                    Pincode = turf.Pincode,
                    Latitude = turf.Latitude,
                    Longitude = turf.Longitude,
                    PricePerSlot = turf.PricePerSlot,
                    SlotDurationMinutes = turf.SlotDurationMinutes,
                    Status = turf.Status.ToString()
                },
                "Turf created successfully.");
        }

        public async Task<ApiResponse<List<TurfResponse>>> GetMyTurfsAsync()
        {
            var owner = await GetCurrentOwnerAsync();

            var turfs = await _context.Turfs
                .AsNoTracking()
                .Where(x => x.OwnerProfileId == owner.Id &&
                            x.Status != TurfStatus.Deleted)
                .Select(x => new TurfResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Address = x.Address,
                    Locality = x.Locality,
                    City = x.City,
                    State = x.State,
                    Pincode = x.Pincode,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    PricePerSlot = x.PricePerSlot,
                    SlotDurationMinutes = x.SlotDurationMinutes,
                    Status = x.Status.ToString()
                })
                .ToListAsync();

            return ApiResponse<List<TurfResponse>>
                .SuccessResponse(turfs, "Turfs retrieved successfully.");
        }

        public async Task<ApiResponse<TurfResponse>> GetByIdAsync(int id)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            var response = new TurfResponse
            {
                Id = turf.Id,
                Name = turf.Name,
                Description = turf.Description,
                Address = turf.Address,
                Locality = turf.Locality,
                City = turf.City,
                State = turf.State,
                Pincode = turf.Pincode,
                Latitude = turf.Latitude,
                Longitude = turf.Longitude,
                PricePerSlot = turf.PricePerSlot,
                SlotDurationMinutes = turf.SlotDurationMinutes,
                Status = turf.Status.ToString()
            };

            return ApiResponse<TurfResponse>
                .SuccessResponse(response, "Turf retrieved successfully.");
        }

        public async Task<ApiResponse<TurfResponse>> UpdateAsync(
    int id,
    CreateTurfRequest request)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            var exists = await _context.Turfs.AnyAsync(x =>
                x.OwnerProfileId == owner.Id &&
                x.Id != id &&
                x.Name.ToLower() == request.Name.Trim().ToLower());

            if (exists)
                throw new BadRequestException("Another turf with this name already exists.");

            if (request.SlotDurationMinutes <= 0)
            {
                throw new BadRequestException(
                    "Slot duration must be greater than 0 minutes.");
            }

            if (request.SlotDurationMinutes > 1440)
            {
                throw new BadRequestException(
                    "Slot duration cannot exceed 24 hours.");
            }

            turf.Name = request.Name.Trim();
            turf.Description = request.Description;
            turf.Address = request.Address;
            turf.Locality = request.Locality;
            turf.City = request.City;
            turf.State = request.State;
            turf.Pincode = request.Pincode;
            turf.Latitude = request.Latitude;
            turf.Longitude = request.Longitude;
            turf.PricePerSlot = request.PricePerSlot;
            turf.SlotDurationMinutes = request.SlotDurationMinutes;
            turf.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new TurfResponse
            {
                Id = turf.Id,
                Name = turf.Name,
                Description = turf.Description,
                Address = turf.Address,
                Locality = turf.Locality,
                City = turf.City,
                State = turf.State,
                Pincode = turf.Pincode,
                Latitude = turf.Latitude,
                Longitude = turf.Longitude,
                PricePerSlot = turf.PricePerSlot,
                SlotDurationMinutes = turf.SlotDurationMinutes,
                Status = turf.Status.ToString()
            };

            return ApiResponse<TurfResponse>
                .SuccessResponse(response, "Turf updated successfully.");
        }

        public async Task<ApiResponse<string>> DeactivateAsync(int id)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            turf.Status = TurfStatus.Deleted;
            turf.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<string>
                .SuccessResponse("Turf deactivated successfully.");
        }


        public async Task<ApiResponse<List<TurfImageResponse>>> UploadImagesAsync(int turfId,List<IFormFile> files)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .Include(x => x.TurfImages)
                .FirstOrDefaultAsync(x =>
                    x.Id == turfId &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            if (files == null || files.Count == 0)
                throw new BadRequestException("Please upload at least one image.");

            if (turf.TurfImages.Count + files.Count > 4)
                throw new BadRequestException("Maximum 4 images are allowed.");

                        var allowedTypes = new[]
                        {
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                };

            var response = new List<TurfImageResponse>();

            foreach (var file in files)
            {
                if (!allowedTypes.Contains(file.ContentType))
                    throw new BadRequestException(
                        $"'{file.FileName}' is not a supported image.");

                if (file.Length > 5 * 1024 * 1024)
                    throw new BadRequestException(
                        $"'{file.FileName}' exceeds 5 MB.");

                var upload = await _imageService.UploadAsync(file);

                var image = new TurfImage
                {
                    TurfId = turf.Id,
                    ImageUrl = upload.ImageUrl,
                    PublicId = upload.PublicId,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    DisplayOrder = turf.TurfImages.Count + 1,
                    IsPrimary = turf.TurfImages.Count == 0
                };

                _context.TurfImages.Add(image);

                turf.TurfImages.Add(image);

                response.Add(new TurfImageResponse
                {
                    ImageUrl = image.ImageUrl,
                    DisplayOrder = image.DisplayOrder,
                    IsPrimary = image.IsPrimary
                });
            }

            await _context.SaveChangesAsync();

            return ApiResponse<List<TurfImageResponse>>
                .SuccessResponse(response,
                    "Images uploaded successfully.");
        }



        public async Task<ApiResponse<string>> UpdateSportsAsync(
    int turfId,
    UpdateTurfSportsRequest request)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .Include(x => x.TurfSports)
                .FirstOrDefaultAsync(x =>
                    x.Id == turfId &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            _context.TurfSports.RemoveRange(turf.TurfSports);

            foreach (var sport in request.Sports.Distinct())
            {
                turf.TurfSports.Add(new TurfSport
                {
                    TurfId = turf.Id,
                    SportType = sport
                });
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse(
                "Sports updated successfully.");
        }


        public async Task<ApiResponse<string>> UpdateAmenitiesAsync(
            int turfId,
            UpdateTurfAmenitiesRequest request)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .Include(x => x.TurfAmenities)
                .FirstOrDefaultAsync(x =>
                    x.Id == turfId &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            _context.TurfAmenities.RemoveRange(turf.TurfAmenities);

            foreach (var amenity in request.Amenities.Distinct())
            {
                turf.TurfAmenities.Add(new TurfAmenity
                {
                    TurfId = turf.Id,
                    Amenity = amenity
                });
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse(
                "Amenities updated successfully.");
        }



        public async Task<ApiResponse<string>> UpdateScheduleAsync(int turfId,UpdateTurfScheduleRequest request)
        {
            var owner = await GetCurrentOwnerAsync();

            var turf = await _context.Turfs
                .Include(x => x.Schedules)
                .FirstOrDefaultAsync(x =>
                    x.Id == turfId &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            if (request.Schedules == null || request.Schedules.Count == 0)
                throw new BadRequestException(
                    "At least one schedule is required.");

            var duplicateDays = request.Schedules
                .GroupBy(x => x.DayOfWeek)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateDays.Any())
            {
                throw new BadRequestException(
                    "Only one schedule is allowed for each day.");
            }

            foreach (var schedule in request.Schedules)
            {
                if (schedule.IsAvailable)
                {
                    if (schedule.StartTime >= schedule.EndTime)
                    {
                        throw new BadRequestException(
                            $"Invalid time range for {schedule.DayOfWeek}.");
                    }
                }
            }

            _context.TurfSchedules.RemoveRange(turf.Schedules);

            foreach (var schedule in request.Schedules)
            {
                turf.Schedules.Add(new TurfSchedule
                {
                    TurfId = turf.Id,
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    IsAvailable = schedule.IsAvailable
                });
            }

            turf.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse(
                "Turf schedule updated successfully.");
        }


        public async Task<ApiResponse<List<TurfScheduleResponse>>> GetScheduleAsync(int turfId)
        {
            var owner = await GetCurrentOwnerAsync();

            var turfExists = await _context.Turfs
                .AnyAsync(x =>
                    x.Id == turfId &&
                    x.OwnerProfileId == owner.Id &&
                    x.Status != TurfStatus.Deleted);

            if (!turfExists)
                throw new NotFoundException("Turf not found.");

            var schedules = await _context.TurfSchedules
                .AsNoTracking()
                .Where(x => x.TurfId == turfId)
                .OrderBy(x => x.DayOfWeek)
                .Select(x => new TurfScheduleResponse
                {
                    DayOfWeek = x.DayOfWeek,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    IsAvailable = x.IsAvailable
                })
                .ToListAsync();

            return ApiResponse<List<TurfScheduleResponse>>
                .SuccessResponse(
                    schedules,
                    "Turf schedule retrieved successfully.");
        }

        public async Task<ApiResponse<List<AvailableSlotResponse>>> GetAvailableSlotsAsync(
    int turfId,
    DateTime date)
        {
            // -----------------------------------------
            // 1. Get active turf
            // -----------------------------------------

            var turf = await _context.Turfs
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == turfId &&
                    x.Status == TurfStatus.Active);

            if (turf == null)
                throw new NotFoundException("Turf not found.");

            // -----------------------------------------
            // 2. Validate slot duration
            // -----------------------------------------

            if (turf.SlotDurationMinutes <= 0 ||
                turf.SlotDurationMinutes > 1440)
            {
                throw new BadRequestException(
                    "Invalid turf slot duration configuration.");
            }

            // -----------------------------------------
            // 3. Selected date
            // -----------------------------------------

            var selectedDate = date.Date;

            var today = DateTime.UtcNow.Date;

            if (selectedDate < today)
            {
                throw new BadRequestException(
                    "You cannot view slots for a past date.");
            }

            // -----------------------------------------
            // 4. Booking advance limit
            // -----------------------------------------

            var maximumDate =
                today.AddDays(turf.BookingAdvanceDays);

            if (selectedDate > maximumDate)
            {
                throw new BadRequestException(
                    $"This turf can only be booked " +
                    $"{turf.BookingAdvanceDays} days in advance.");
            }

            // -----------------------------------------
            // 5. Find weekly schedule
            // -----------------------------------------

            var dayOfWeek = selectedDate.DayOfWeek;

            var schedule = await _context.TurfSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TurfId == turfId &&
                    x.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsAvailable)
            {
                return ApiResponse<List<AvailableSlotResponse>>
                    .SuccessResponse(
                        new List<AvailableSlotResponse>(),
                        "Turf is closed on this day.");
            }

            // -----------------------------------------
            // 6. Validate schedule
            // -----------------------------------------

            if (schedule.StartTime >= schedule.EndTime)
            {
                throw new BadRequestException(
                    "Invalid turf schedule configuration.");
            }

            // -----------------------------------------
            // 7. Expire old pending bookings
            // -----------------------------------------

            var now = DateTime.UtcNow;

            var expiredBookings = await _context.Bookings
                .Include(x => x.BookingSlots)
                .Where(x =>
                    x.TurfId == turfId &&
                    x.BookingDate == selectedDate &&
                    x.Status == BookingStatus.Pending &&
                    x.ExpiresAt.HasValue &&
                    x.ExpiresAt.Value <= now &&
                    x.BookingSlots.Any(s => s.IsActive))
                .ToListAsync();

            if (expiredBookings.Count > 0)
            {
                foreach (var booking in expiredBookings)
                {
                    _bookingLifecycleService.ExpireBooking(booking);
                }

                await _context.SaveChangesAsync();
            }

            // -----------------------------------------
            // 8. Get currently active booked slots
            // -----------------------------------------

            var bookedStartTimes = await _context.BookingSlots
                .AsNoTracking()
                .Where(x =>
                    x.TurfId == turfId &&
                    x.BookingDate == selectedDate &&
                    x.IsActive)
                .Select(x => x.StartTime)
                .ToListAsync();

            var bookedSlotSet =
                bookedStartTimes.ToHashSet();

            // -----------------------------------------
            // 9. Generate all possible slots
            // -----------------------------------------

            var slots =
                new List<AvailableSlotResponse>();

            var scheduleStart =
                schedule.StartTime.ToTimeSpan();

            var scheduleEnd =
                schedule.EndTime.ToTimeSpan();

            var currentTime = scheduleStart;

            var slotDuration =
                TimeSpan.FromMinutes(
                    turf.SlotDurationMinutes);

            const int maxSlots = 96;

            var slotCount = 0;

            while (currentTime < scheduleEnd)
            {
                // Safety protection
                if (slotCount >= maxSlots)
                {
                    throw new BadRequestException(
                        "Too many slots configured for this schedule.");
                }

                var slotEnd =
                    currentTime + slotDuration;

                // Don't create an incomplete slot
                // beyond the owner's schedule.
                if (slotEnd > scheduleEnd)
                {
                    break;
                }

                var currentTimeOnly =
                    TimeOnly.FromTimeSpan(currentTime);

                var slotEndTimeOnly =
                    TimeOnly.FromTimeSpan(slotEnd);

                var isBooked =
                    bookedSlotSet.Contains(
                        currentTimeOnly);

                slots.Add(new AvailableSlotResponse
                {
                    StartTime = currentTimeOnly,
                    EndTime = slotEndTimeOnly,
                    Price = turf.PricePerSlot,
                    IsAvailable = !isBooked
                });

                currentTime = slotEnd;

                slotCount++;
            }

            return ApiResponse<List<AvailableSlotResponse>>
                .SuccessResponse(
                    slots,
                    "Available slots retrieved successfully.");
        }


        private static double CalculateDistanceKm(
    double latitude1,
    double longitude1,
    double latitude2,
    double longitude2)
        {
            const double earthRadiusKm = 6371.0;

            var dLatitude =
                DegreesToRadians(latitude2 - latitude1);

            var dLongitude =
                DegreesToRadians(longitude2 - longitude1);

            var a =
                Math.Sin(dLatitude / 2) *
                Math.Sin(dLatitude / 2)
                +
                Math.Cos(DegreesToRadians(latitude1)) *
                Math.Cos(DegreesToRadians(latitude2)) *
                Math.Sin(dLongitude / 2) *
                Math.Sin(dLongitude / 2);

            var c =
                2 * Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public async Task<ApiResponse<List<NearbyTurfResponse>>> GetNearbyTurfsAsync(
    NearbyTurfRequest request)
        {
            if (request.Latitude < -90 || request.Latitude > 90)
                throw new BadRequestException("Invalid latitude.");

            if (request.Longitude < -180 || request.Longitude > 180)
                throw new BadRequestException("Invalid longitude.");

            if (request.RadiusKm <= 0)
                throw new BadRequestException(
                    "Radius must be greater than zero.");

            if (request.RadiusKm > 50)
                throw new BadRequestException(
                    "Maximum search radius is 50 km.");

            // Approximate bounding box.
            // This allows SQL Server to remove obviously distant turfs
            // before we calculate the exact distance.
            var latitudeRange = request.RadiusKm / 111.0;

            var longitudeRange =
                request.RadiusKm /
                (111.0 * Math.Cos(
                    (double)request.Latitude * Math.PI / 180.0));

            var minLatitude =
                (double)request.Latitude - latitudeRange;

            var maxLatitude =
                (double)request.Latitude + latitudeRange;

            var minLongitude =
                (double)request.Longitude - longitudeRange;

            var maxLongitude =
                (double)request.Longitude + longitudeRange;

            var turfs = await _context.Turfs
                .AsNoTracking()
                .Where(x =>
                    x.Status == TurfStatus.Active &&
                    (double)x.Latitude >= minLatitude &&
                    (double)x.Latitude <= maxLatitude &&
                    (double)x.Longitude >= minLongitude &&
                    (double)x.Longitude <= maxLongitude)
                .Select(x => new NearbyTurfResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Address = x.Address,
                    Locality = x.Locality,
                    City = x.City,
                    State = x.State,
                    Pincode = x.Pincode,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    PricePerSlot = x.PricePerSlot,
                    SlotDurationMinutes = x.SlotDurationMinutes
                })
                .ToListAsync();

            var userLatitude = (double)request.Latitude;
            var userLongitude = (double)request.Longitude;

            foreach (var turf in turfs)
            {
                turf.DistanceKm = CalculateDistanceKm(
                    userLatitude,
                    userLongitude,
                    (double)turf.Latitude,
                    (double)turf.Longitude);
            }

            var result = turfs
                .Where(x => x.DistanceKm <= request.RadiusKm)
                .OrderBy(x => x.DistanceKm)
                .ToList();

            return ApiResponse<List<NearbyTurfResponse>>
                .SuccessResponse(
                    result,
                    "Nearby turfs retrieved successfully.");
        }


        public async Task<ApiResponse<List<NearbyTurfResponse>>> SearchTurfsAsync(
    TurfSearchRequest request)
        {
            if (request.MaxPrice.HasValue &&
                request.MaxPrice.Value < 0)
            {
                throw new BadRequestException(
                    "Maximum price cannot be negative.");
            }

            if (request.RadiusKm <= 0)
            {
                throw new BadRequestException(
                    "Radius must be greater than zero.");
            }

            if (request.RadiusKm > 50)
            {
                throw new BadRequestException(
                    "Maximum search radius is 50 km.");
            }

            if (request.Latitude.HasValue &&
                (request.Latitude < -90 || request.Latitude > 90))
            {
                throw new BadRequestException(
                    "Invalid latitude.");
            }

            if (request.Longitude.HasValue &&
                (request.Longitude < -180 || request.Longitude > 180))
            {
                throw new BadRequestException(
                    "Invalid longitude.");
            }

            if (request.Latitude.HasValue != request.Longitude.HasValue)
            {
                throw new BadRequestException(
                    "Latitude and longitude must be provided together.");
            }

            // ------------------------------------------------
            // Base query
            // ------------------------------------------------

            var query = _context.Turfs
                .AsNoTracking()
                .Where(x => x.Status == TurfStatus.Active);

            // ------------------------------------------------
            // Price filter
            // ------------------------------------------------

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(x =>
                    x.PricePerSlot <= request.MaxPrice.Value);
            }

            // ------------------------------------------------
            // City filter
            // ------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();

                query = query.Where(x =>
                    x.City.ToLower() == city.ToLower());
            }

            // ------------------------------------------------
            // Locality filter
            // ------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Locality))
            {
                var locality = request.Locality.Trim();

                query = query.Where(x =>
                    x.Locality.ToLower() == locality.ToLower());
            }

            // ------------------------------------------------
            // Sport filter
            // ------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Sport))
            {
                if (!Enum.TryParse<SportType>(
                        request.Sport.Trim(),
                        true,
                        out var sportType))
                {
                    throw new BadRequestException(
                        $"Invalid sport '{request.Sport}'.");
                }

                query = query.Where(x =>
                    x.TurfSports.Any(ts =>
                        ts.SportType == sportType));
            }

            // ------------------------------------------------
            // Location bounding box
            // ------------------------------------------------

            if (request.Latitude.HasValue &&
                request.Longitude.HasValue)
            {
                var latitude = (double)request.Latitude.Value;
                var longitude = (double)request.Longitude.Value;

                var latitudeRange =
                    request.RadiusKm / 111.0;

                var cosLatitude =
                    Math.Cos(latitude * Math.PI / 180.0);

                // Prevent division by zero near the poles
                if (Math.Abs(cosLatitude) < 0.01)
                    throw new BadRequestException(
                        "Location is not supported for this search.");

                var longitudeRange =
                    request.RadiusKm /
                    (111.0 * cosLatitude);

                var minLatitude = latitude - latitudeRange;
                var maxLatitude = latitude + latitudeRange;

                var minLongitude = longitude - longitudeRange;
                var maxLongitude = longitude + longitudeRange;

                query = query.Where(x =>
                    (double)x.Latitude >= minLatitude &&
                    (double)x.Latitude <= maxLatitude &&
                    (double)x.Longitude >= minLongitude &&
                    (double)x.Longitude <= maxLongitude);
            }

            // ------------------------------------------------
            // Fetch database results
            // ------------------------------------------------

            var turfs = await query
                .Select(x => new NearbyTurfResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Address = x.Address,
                    Locality = x.Locality,
                    City = x.City,
                    State = x.State,
                    Pincode = x.Pincode,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    PricePerSlot = x.PricePerSlot,
                    SlotDurationMinutes = x.SlotDurationMinutes,

                    Sports = x.TurfSports
                    .Select(ts => ts.SportType.ToString())
                    .ToList()
                })
                .ToListAsync();

            // ------------------------------------------------
            // Exact distance calculation
            // ------------------------------------------------

            if (request.Latitude.HasValue &&
                request.Longitude.HasValue)
            {
                var userLatitude =
                    (double)request.Latitude.Value;

                var userLongitude =
                    (double)request.Longitude.Value;

                foreach (var turf in turfs)
                {
                    turf.DistanceKm = CalculateDistanceKm(
                        userLatitude,
                        userLongitude,
                        (double)turf.Latitude,
                        (double)turf.Longitude);
                }

                turfs = turfs
                    .Where(x => x.DistanceKm <= request.RadiusKm)
                    .ToList();
            }

            // ------------------------------------------------
            // Sorting
            // ------------------------------------------------

            turfs = request.SortBy.Trim().ToLower() switch
            {
                "price" =>
                    turfs
                        .OrderBy(x => x.PricePerSlot)
                        .ThenBy(x => x.DistanceKm)
                        .ToList(),

                "distance" =>
                    turfs
                        .OrderBy(x => x.DistanceKm)
                        .ThenBy(x => x.PricePerSlot)
                        .ToList(),

                _ => throw new BadRequestException(
                    "Invalid SortBy value. Use 'distance' or 'price'.")
            };

            return ApiResponse<List<NearbyTurfResponse>>
                .SuccessResponse(
                    turfs,
                    "Turfs retrieved successfully.");
        }


    }
}
