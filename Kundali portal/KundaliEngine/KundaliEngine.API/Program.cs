using HoraLife.Store.Application.DependencyInjection;
using HoraLife.Store.Infrastructure.DependencyInjection;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.AdvancedStrength;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.CurrentLifeFocus;
using KundaliEngine.API.Services.Dashboard;
using KundaliEngine.API.Services.Email;
using KundaliEngine.API.Services.Learning;
using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.API.Services.Recommendations;
using KundaliEngine.API.Services.ReportPdf;
using KundaliEngine.API.Services.ReportPdf.Html;
using KundaliEngine.API.Services.ReportShare;
using KundaliEngine.API.Services.YogaDoshaActivation;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Astrology.Services.SudarshanChakra;
using KundaliEngine.Infrastructure;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register core astrology services for DI (Phase 1 foundation)
builder.Services.AddSingleton<IEphemerisProvider, SwissEphemerisProvider>();
builder.Services.AddSingleton<ChartService>();
builder.Services.AddSingleton<PlanetScoreService>();
builder.Services.AddSingleton<HouseScoreService>();
builder.Services.AddSingleton<DashaService>();
builder.Services.AddSingleton<PanchangService>();
builder.Services.AddSingleton<AspectService>();
builder.Services.AddSingleton<PlanetStrengthService>();
builder.Services.AddSingleton<OverallKundaliScoreService>();
builder.Services.AddSingleton<RemediesRecommendationService>();
builder.Services.AddSingleton<CompatibilityScoreService>();
builder.Services.AddSingleton<CompatibilityDoshaAnalysisService>();
builder.Services.AddSingleton<SudarshanChakraService>();
builder.Services.AddSingleton<YogaDoshaActivationService>();
builder.Services.AddSingleton<AdvancedStrengthTableService>();
builder.Services.AddSingleton<AstrologyKnowledgeService>();
builder.Services.AddSingleton<IYogaRule, GajakesariYogaRule>();
builder.Services.AddSingleton<IYogaRule, BudhaAdityaYogaRule>();
builder.Services.AddSingleton<IYogaRule, ChandraMangalaYogaRule>();
builder.Services.AddSingleton<IYogaRule, RuchakaYogaRule>();
builder.Services.AddSingleton<IYogaRule, BhadraYogaRule>();
builder.Services.AddSingleton<IYogaRule, HamsaYogaRule>();
builder.Services.AddSingleton<IYogaRule, MalavyaYogaRule>();
builder.Services.AddSingleton<IYogaRule, SasaYogaRule>();
builder.Services.AddSingleton<IYogaRule, ShakataYogaRule>();
builder.Services.AddSingleton<IYogaRule, ParivartanaYogaRule>();
builder.Services.AddSingleton<IYogaRule, KendraTrikonaRajaYogaRule>();
builder.Services.AddSingleton<IYogaRule, LakshmiYogaRule>();
builder.Services.AddSingleton<IYogaRule, SaraswatiYogaRule>();
builder.Services.AddSingleton<IYogaRule, AdhiYogaRule>();
builder.Services.AddSingleton<IYogaRule, DurdharaYogaRule>();
builder.Services.AddSingleton<IYogaRule, AmalaYogaRule>();
builder.Services.AddSingleton<IYogaRule, ShubhaKartariYogaRule>();
builder.Services.AddSingleton<IYogaRule, PapaKartariYogaRule>();
builder.Services.AddSingleton<YogaDetectionService>();
builder.Services.AddSingleton<IDoshaRule, ManglikDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, KaalSarpaDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, ChandraGrahanDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, SuryaGrahanDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, GuruChandalDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, PitraDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, ShaniDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, KemadrumaDoshaRule>();
builder.Services.AddSingleton<IDoshaRule, DaridraDoshaRule>();
builder.Services.AddSingleton<DoshaDetectionService>();

builder.Services.AddSingleton(_ =>
{
    var yogaMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "KundaliYoga", "Yogas.json");
    return new YogaMetadataService(yogaMetadataPath);
});
builder.Services.AddSingleton(_ =>
{
    var doshaMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "KundaliDosha", "Doshas.json");
    return new DoshaMetadataService(doshaMetadataPath);
});
builder.Services.AddSingleton(_ =>
{
    var nakshatraMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Nakshatra");
    return new NakshatraMetadataService(nakshatraMetadataPath);
});
builder.Services.AddSingleton(_ =>
{
    var rashiMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Rashi");
    return new RashiMetadataService(rashiMetadataPath);
});
builder.Services.AddSingleton(_ =>
{
    var yoniCompatibilityPath = Path.Combine(AppContext.BaseDirectory, "Data", "Compatibility", "YoniCompatibility.json");
    return new YoniCompatibilityService(yoniCompatibilityPath);
});
builder.Services.AddSingleton(_ =>
{
    var muhurthaMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Muhurtha");
    return new MuhurthaMetadataService(muhurthaMetadataPath);
});
builder.Services.AddSingleton<IMuhurthaRuleConfigurationProvider, MuhurthaRuleConfigurationProvider>();
builder.Services.AddSingleton<IMuhurthaSunriseSunsetService, MuhurthaSunriseSunsetService>();
builder.Services.AddSingleton<IMuhurthaGenericTimeWindowService, MuhurthaGenericTimeWindowService>();
builder.Services.AddSingleton<IVastuMuhurthaEligibilityService, VastuMuhurthaEligibilityService>();
builder.Services.AddSingleton<IMuhurthaRuleEvaluationService, MuhurthaRuleEvaluationService>();
builder.Services.AddSingleton<IMuhurthaCandidateGenerator, MuhurthaCandidateGenerator>();
builder.Services.AddSingleton<IMuhurthaScoringService, MuhurthaScoringService>();
builder.Services.AddSingleton<IMuhurthaRecommendationService, MuhurthaRecommendationService>();
builder.Services.AddSingleton<IMuhurthaService, MuhurthaService>();
builder.Services.AddSingleton(_ =>
{
    var currentLifeFocusPath = Path.Combine(AppContext.BaseDirectory, "Data", "CurrentLifeFocus");
    return new CurrentLifeFocusMetadataService(currentLifeFocusPath);
});
builder.Services.AddSingleton<IHouseActivationScorer, HouseActivationScorer>();
builder.Services.AddSingleton<ILifeAreaMapper, LifeAreaMapper>();
builder.Services.AddSingleton<ICurrentLifeFocusAnalysisStore, CurrentLifeFocusAnalysisStore>();
builder.Services.AddScoped<ICurrentLifeFocusService, CurrentLifeFocusService>();
builder.Services.AddSingleton<IFocusedLifeAreaNarrativeBuilder, FocusedLifeAreaNarrativeBuilder>();
builder.Services.AddScoped<IFocusedLifeAreaAnalysisService, FocusedLifeAreaAnalysisService>();
builder.Services.Configure<ReportShareSmtpOptions>(builder.Configuration.GetSection("ReportShare:Smtp"));
builder.Services.AddSingleton<IReportHtmlTemplateService, ReportHtmlTemplateService>();
builder.Services.AddSingleton<IHtmlReportPdfRenderer, ChromeHtmlReportPdfRenderer>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IReportShareEmailService, ReportShareEmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICommunicationLogService, CommunicationLogService>();
builder.Services.AddSingleton<ILearningContentService, LearningContentService>();
builder.Services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthOtpService, AuthOtpService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddStoreApplication();
builder.Services.AddStoreInfrastructure(builder.Configuration);


builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

//JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is missing in appsettings.json");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };
    options.AddPolicy("AuthOtp", httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var partitionKey = string.IsNullOrWhiteSpace(forwardedFor) ? $"ip:{remoteIp}" : $"ip:{forwardedFor}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });
    options.AddPolicy("ReportShare", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = string.IsNullOrWhiteSpace(userId)
            ? $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"
            : $"user:{userId}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
           
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("HoraLifeFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                var isLocalhost =
                    uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("127.0.0.1");

                var isHoraLife =
                    origin.Equals("https://horalife.in", StringComparison.OrdinalIgnoreCase) ||
                    origin.Equals("https://www.horalife.in", StringComparison.OrdinalIgnoreCase) ||
                    origin.Equals("http://horalife.in", StringComparison.OrdinalIgnoreCase) ||
                    origin.Equals("http://www.horalife.in", StringComparison.OrdinalIgnoreCase);

                return isLocalhost || isHoraLife;
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
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
builder.Services.AddAuthorization();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("HoraLifeFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();


app.MapControllers();

app.Run();






