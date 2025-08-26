using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels;
using Serilog;
using System.Globalization;
using System.IO;
using AutoMapper;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Application.Services;
using PropertyManagement.Application.DTOs;

// Ensure log directory exists before Serilog is configured
Directory.CreateDirectory("/app/logs");

// Build configuration first
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from configuration (appsettings.json, environment variables, etc.)
builder.Host.UseSerilog((context, services, configuration) =>
{
  configuration
      .ReadFrom.Configuration(context.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext();
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Require authentication globally (force login for all pages except [AllowAnonymous])
builder.Services.AddAuthorization(options =>
{
  options.FallbackPolicy = new AuthorizationPolicyBuilder()
      .RequireAuthenticatedUser()
      .Build();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(0)
    ));

// Configure FluentValidation with manual validation for async rules
builder.Services.AddFluentValidationAutoValidation(options =>
{
    options.DisableDataAnnotationsValidation = true;
    // Disable implicit child validation to avoid async issues
    options.ImplicitlyValidateChildProperties = false;
}).AddFluentValidationClientsideAdapters();

builder.Services.AddHttpClient();
builder.Services.AddScoped<ISmsService, BulkSmsService>();
builder.Services.AddHostedService<RentReminderService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
// Register all validators for DI (required for validators with constructor injection)
builder.Services.AddScoped<IValidator<Payment>, PaymentValidator>();
builder.Services.AddScoped<IValidator<Tenant>, TenantValidator>();
builder.Services.AddScoped<IValidator<MaintenanceRequest>, MaintenanceRequestValidator>();
builder.Services.AddScoped<IValidator<LeaseAgreement>, LeaseAgreementValidator>();
builder.Services.AddScoped<IValidator<Inspection>, InspectionValidator>();
builder.Services.AddScoped<IValidator<UtilityBill>, UtilityBillValidator>();
builder.Services.AddScoped<IValidator<BookingRequestViewModel>, BookingRequestViewModelValidator>();
builder.Services.AddScoped<IValidator<TenantLoginViewModel>, TenantLoginViewModelValidator>();
builder.Services.AddScoped<IValidator<RoomFormViewModel>, RoomFormViewModelValidator>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Register Application Services
builder.Services.AddScoped<ITenantApplicationService, TenantApplicationService>();
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();
builder.Services.AddScoped<ILeaseAgreementApplicationService, LeaseAgreementApplicationService>();
builder.Services.AddScoped<IRoomApplicationService, RoomApplicationService>();
builder.Services.AddScoped<IBookingRequestApplicationService, BookingRequestApplicationService>();
builder.Services.AddScoped<IMaintenanceRequestApplicationService, MaintenanceRequestApplicationService>();
builder.Services.AddScoped<IInspectionApplicationService, InspectionApplicationService>();
builder.Services.AddScoped<IUtilityBillApplicationService, UtilityBillApplicationService>();
builder.Services.AddScoped<ILeaseGenerationService, LeaseGenerationService>();
builder.Services.AddScoped<IWaitingListApplicationService, WaitingListApplicationService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.LoginPath = "/Tenants/Login";
      options.LogoutPath = "/Tenants/Logout";
      options.AccessDeniedPath = "/Tenants/AccessDenied";
      // Change the cookie name on every app start to force logout for all users
      options.Cookie.Name = "PropertyManagementAuth";
    });

builder.Services.AddAutoMapper(cfg =>
{
  // Payment mappings
  cfg.CreateMap<PaymentViewModel, Payment>()
      .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.PaymentId ?? 0))
      .ForMember(dest => dest.Date, opt => opt.Ignore()); // Set in controller
  cfg.CreateMap<Payment, PaymentViewModel>();

  // Room mappings
  cfg.CreateMap<Room, RoomViewModel>().ReverseMap();
  cfg.CreateMap<RoomFormViewModel, Room>().ReverseMap();

  cfg.CreateMap<LeaseAgreement, LeaseAgreementViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ReverseMap();

  // User mappings - Entity to ViewModel
  cfg.CreateMap<User, UserViewModel>().ReverseMap();
  
  // User mappings - DTO to ViewModel
  cfg.CreateMap<UserDto, UserViewModel>()
      .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // PasswordHash is not in UserDto
      .ReverseMap()
      .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
      .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
      .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));
  
  cfg.CreateMap<Tenant, TenantViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ForMember(dest => dest.LeaseAgreements, opt => opt.MapFrom(src => src.LeaseAgreements))
      .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments))
      .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
  cfg.CreateMap<TenantViewModel, Tenant>()
      .ForMember(dest => dest.Room, opt => opt.Ignore())
      .ForMember(dest => dest.LeaseAgreements, opt => opt.Ignore())
      .ForMember(dest => dest.Payments, opt => opt.Ignore())
      .ForMember(dest => dest.User, opt => opt.Ignore());

  // BookingRequest mappings
  cfg.CreateMap<BookingRequest, BookingRequestViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ForMember(dest => dest.RoomOptions, opt => opt.Ignore());
  cfg.CreateMap<BookingRequestViewModel, BookingRequest>()
      .ForMember(dest => dest.Room, opt => opt.Ignore());
  
  // Inspection mappings
  cfg.CreateMap<Inspection, InspectionViewModel>().ReverseMap();
  cfg.CreateMap<InspectionViewModel, Inspection>()
      .ForMember(dest => dest.Room, opt => opt.Ignore());
  
  // MaintenanceRequest mappings
  cfg.CreateMap<MaintenanceRequest, MaintenanceRequestViewModel>().ReverseMap();
  cfg.CreateMap<MaintenanceRequestViewModel, MaintenanceRequest>()
      .ForMember(dest => dest.Room, opt => opt.Ignore());

  // Application DTOs mappings (Entity <-> DTO)
  cfg.CreateMap<Tenant, TenantDto>().ReverseMap();
  cfg.CreateMap<User, UserDto>().ReverseMap();
  cfg.CreateMap<Room, RoomDto>().ReverseMap();
  cfg.CreateMap<Room, RoomWithTenantsDto>().ReverseMap();
  cfg.CreateMap<Payment, PaymentDto>().ReverseMap();
  cfg.CreateMap<LeaseAgreement, LeaseAgreementDto>().ReverseMap();
  cfg.CreateMap<BookingRequest, BookingRequestDto>().ReverseMap();
  cfg.CreateMap<MaintenanceRequest, MaintenanceRequestDto>().ReverseMap();
  cfg.CreateMap<Inspection, InspectionDto>().ReverseMap();
  cfg.CreateMap<UtilityBill, UtilityBillDto>().ReverseMap();
  
  // Create/Update DTO mappings
  cfg.CreateMap<CreateRoomDto, Room>();
  cfg.CreateMap<UpdateRoomDto, Room>();
  cfg.CreateMap<CreateBookingRequestDto, BookingRequest>();
  cfg.CreateMap<UpdateBookingRequestDto, BookingRequest>();
  cfg.CreateMap<CreateMaintenanceRequestDto, MaintenanceRequest>();
  cfg.CreateMap<UpdateMaintenanceRequestDto, MaintenanceRequest>();
  cfg.CreateMap<CreateInspectionDto, Inspection>();
  cfg.CreateMap<UpdateInspectionDto, Inspection>();
  cfg.CreateMap<CreateUtilityBillDto, UtilityBill>();
  cfg.CreateMap<UpdateUtilityBillDto, UtilityBill>();
  
  // DTO to ViewModel mappings (CRITICAL MISSING MAPPINGS ADDED)
  cfg.CreateMap<TenantDto, TenantViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
  
  // MISSING: LeaseAgreementDto to LeaseAgreementViewModel mapping
  cfg.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
      .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ForMember(dest => dest.File, opt => opt.Ignore()) // IFormFile is not in DTO
      .ForMember(dest => dest.RentDueDate, opt => opt.Ignore()); // Computed property
  
  // MISSING: PaymentDto to PaymentViewModel mapping with proper nested object handling
  cfg.CreateMap<PaymentDto, PaymentViewModel>()
      .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
      .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
      .ForMember(dest => dest.LeaseAgreement, opt => opt.MapFrom(src => src.LeaseAgreement))
      .ForMember(dest => dest.Room, opt => opt.Ignore()); // Room comes through Tenant navigation
  
  cfg.CreateMap<RoomDto, RoomViewModel>();
  cfg.CreateMap<RoomWithTenantsDto, RoomViewModel>();
  cfg.CreateMap<BookingRequestDto, BookingRequestViewModel>();
  cfg.CreateMap<InspectionDto, InspectionViewModel>();
  cfg.CreateMap<MaintenanceRequestDto, MaintenanceRequestViewModel>();
  
  // FIXED: UtilityBillDto to UtilityBillViewModel mappings
  cfg.CreateMap<UtilityBillDto, UtilityBillViewModel>()
      .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
      .ForMember(dest => dest.RoomOptions, opt => opt.Ignore());
  cfg.CreateMap<UtilityBillDto, UtilityBillFormViewModel>()
      .ForMember(dest => dest.RoomOptions, opt => opt.Ignore())
      .ForMember(dest => dest.WaterRate, opt => opt.Ignore())
      .ForMember(dest => dest.ElectricityRate, opt => opt.Ignore());
  
  // MaintenanceRequest DTO to ViewModel mappings
  cfg.CreateMap<MaintenanceRequestDto, MaintenanceRequestFormViewModel>()
      .ForMember(dest => dest.RoomOptions, opt => opt.Ignore())
      .ForMember(dest => dest.StatusOptions, opt => opt.Ignore());
  
  // ViewModel to DTO mappings
  cfg.CreateMap<TenantViewModel, CreateTenantDto>();
  cfg.CreateMap<TenantViewModel, UpdateTenantDto>();
  cfg.CreateMap<TenantViewModel, RegisterTenantDto>();
  cfg.CreateMap<PaymentViewModel, CreatePaymentDto>();
  cfg.CreateMap<PaymentViewModel, UpdatePaymentDto>();
  cfg.CreateMap<RoomFormViewModel, CreateRoomDto>();
  cfg.CreateMap<RoomFormViewModel, UpdateRoomDto>();
  cfg.CreateMap<BookingRequestViewModel, CreateBookingRequestDto>();
  cfg.CreateMap<BookingRequestViewModel, UpdateBookingRequestDto>();
  cfg.CreateMap<InspectionViewModel, CreateInspectionDto>();
  cfg.CreateMap<InspectionViewModel, UpdateInspectionDto>();
  cfg.CreateMap<MaintenanceRequestViewModel, CreateMaintenanceRequestDto>();
  cfg.CreateMap<MaintenanceRequestViewModel, UpdateMaintenanceRequestDto>();

  // Digital Lease mappings
  cfg.CreateMap<LeaseTemplate, LeaseTemplateDto>().ReverseMap();
  cfg.CreateMap<DigitalSignature, DigitalSignatureDto>().ReverseMap();
  cfg.CreateMap<DigitalSignatureDto, DigitalSignatureViewModel>().ReverseMap();
  cfg.CreateMap<LeaseTemplateDto, LeaseTemplateViewModel>().ReverseMap();
  cfg.CreateMap<LeaseTemplateViewModel, CreateLeaseTemplateDto>();
  cfg.CreateMap<LeaseTemplateViewModel, UpdateLeaseTemplateDto>();
  cfg.CreateMap<CreateLeaseTemplateDto, LeaseTemplate>();
  cfg.CreateMap<UpdateLeaseTemplateDto, LeaseTemplate>();

  // Waiting List mappings
  // Entity to DTO mappings
  cfg.CreateMap<WaitingListEntry, WaitingListEntryDto>().ReverseMap();
  cfg.CreateMap<WaitingListNotification, WaitingListNotificationDto>().ReverseMap();
  
  // Create/Update DTO mappings
  cfg.CreateMap<CreateWaitingListEntryDto, WaitingListEntry>();
  cfg.CreateMap<UpdateWaitingListEntryDto, WaitingListEntry>();
  
  // DTO to ViewModel mappings
  cfg.CreateMap<WaitingListEntryDto, WaitingListEntryViewModel>().ReverseMap();
  cfg.CreateMap<WaitingListNotificationDto, WaitingListNotificationViewModel>().ReverseMap();
  cfg.CreateMap<WaitingListSummaryDto, WaitingListSummaryViewModel>().ReverseMap();
  
  // ViewModel to Create/Update DTO mappings
  cfg.CreateMap<WaitingListEntryViewModel, CreateWaitingListEntryDto>();
  cfg.CreateMap<WaitingListEntryViewModel, UpdateWaitingListEntryDto>();
  cfg.CreateMap<QuickAddWaitingListViewModel, CreateWaitingListEntryDto>();
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

builder.WebHost.ConfigureKestrel(options =>
{
  if (!builder.Environment.IsDevelopment())
  {
    options.ListenAnyIP(80); // HTTP for production
    // Only configure HTTPS if certificate exists
    if (File.Exists("/https/aspnetapp.pfx"))
    {
      options.ListenAnyIP(443, listenOptions =>
      {
        listenOptions.UseHttps("/https/aspnetapp.pfx", "YourPassword123");
      });
    }
  }
});
var app = builder.Build();

// Use Prometheus metrics endpoint
app.UseMetricServer(); // Exposes /metrics
app.UseHttpMetrics();  // Collects default HTTP metrics

// Seed initial data
using (var scope = app.Services.CreateScope())
{
  var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  context.Database.Migrate();

  // Read the setting from configuration
  var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
  var enableSeeding = config.GetValue<bool>("EnableDatabaseSeeding");

  if (enableSeeding)
  {
    DatabaseSeeder.Seed(context);
  }
  // Check if a manager already exists
  if (!context.Users.Any(u => u.Role == "Manager"))
  {
    var manager = new User
    {
      Username = "Admin",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("01Pa$$w0rd2025#"),
      Role = "Manager"
    };
    context.Users.Add(manager);
    context.SaveChanges();
  }

}

// Set default culture to South Africa
var defaultCulture = new CultureInfo("en-ZA");
var localizationOptions = new RequestLocalizationOptions
{
  DefaultRequestCulture = new RequestCulture(defaultCulture),
  SupportedCultures = new List<CultureInfo> { defaultCulture },
  SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler(errorApp =>
  {
    errorApp.Run(async context =>
      {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exceptionHandlerPathFeature?.Error, "Unhandled exception occurred");
        context.Response.Redirect("/Home/Error");
      });
  });
  app.UseHsts();
}

// Only redirect to HTTPS if HTTPS is configured
if (!app.Environment.IsDevelopment() && File.Exists("/https/aspnetapp.pfx"))
{
  app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints (before other routes)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  AllowCachingResponses = false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  AllowCachingResponses = false,
  Predicate = _ => true
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  AllowCachingResponses = false,
  Predicate = _ => false // Only basic liveness, no dependency checks
}).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Test log from PropertyManagement.Web startup");

app.Run();