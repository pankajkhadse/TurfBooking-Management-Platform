using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TurfBooking.API.Data;
using TurfBooking.API.Data.Seeders;
using TurfBooking.API.Extensions;
using TurfBooking.API.Helpers;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Services;
using TurfBooking.API.Services.Background;
using TurfBooking.API.Services.Implementations;
using TurfBooking.API.Services.Interfaces;
using TurfBooking.API.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TurfBooking API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token.\n\nExample:\nBearer eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();




builder.Services
.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("Jwt");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["Key"]!))
    };
});





builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminSeeder>();

builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection(AdminSettings.SectionName));

builder.Services.Configure<RazorpaySettings>(
    builder.Configuration.GetSection(RazorpaySettings.SectionName));

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection(CloudinarySettings.SectionName));


builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITurfService, TurfService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingLifecycleService, BookingLifecycleService>();
builder.Services.AddScoped<IImageService, CloudinaryImageService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWebhookEventProcessingService,WebhookEventProcessingService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRefundService, RefundService>();

builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection(BookingSettings.SectionName));

builder.Services.AddHostedService<BookingExpirationService>();
builder.Services.AddHostedService<WebhookEventProcessingWorker>();

//temporary for payment widget
builder.Services.AddCors(options =>
{
    options.AddPolicy("TemporaryPaymentUI", policy =>
    {
        policy
            .WithOrigins("http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();  

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var adminSeeder = services.GetRequiredService<AdminSeeder>();

    await RoleSeeder.SeedRolesAsync(roleManager);

    await adminSeeder.SeedAsync();
    await AmenitySeeder.SeedAsync(context);

}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();    
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseGlobalExceptionMiddleware();

//temporary for payment widget
app.UseCors("TemporaryPaymentUI");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.Run();
