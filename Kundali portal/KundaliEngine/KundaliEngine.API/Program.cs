using HoraLife.PredictionEngine;
using HoraLife.PredictionEngine.Services;
using HoraLife.Store.Application.DependencyInjection;
using HoraLife.Store.Infrastructure.DependencyInjection;
using KundaliEngine.API.DTOs.Responses.Numerology;
using KundaliEngine.API.Prediction.Mappers;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Admin;
using KundaliEngine.API.Services.AdvancedStrength;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.Billing;
using KundaliEngine.API.Services.CurrentLifeFocus;
using KundaliEngine.API.Services.Horoscope;
using KundaliEngine.API.Services.Dashboard;
using KundaliEngine.API.Services.Email;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.API.Services.Gochar;
using KundaliEngine.API.Services.Learning;
using KundaliEngine.API.Services.LifeArea;
using KundaliEngine.API.Services.Locations;
using KundaliEngine.API.Services.Panchang;
using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.API.Services.Muhurtha.GrahaPravesha;
using KundaliEngine.API.Services.Recommendations;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.API.Services.ReportPdf;
using KundaliEngine.API.Services.ReportPdf.Html;
using KundaliEngine.API.Services.ReportShare;
using KundaliEngine.API.Services.Sms;
using KundaliEngine.API.Services.YogaDoshaActivation;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.DivisionalCharts;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.KnowledgeBase.Services;
using KundaliEngine.KnowledgeBase.Services.Numerology;
using KundaliEngine.Astrology.Services.SudarshanChakra;
using KundaliEngine.Infrastructure;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Timeouts;
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
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(3);
});
DotEnvLoader.Load(
    builder.Environment.ContentRootPath,
    Directory.GetCurrentDirectory(),
    AppContext.BaseDirectory);
builder.Configuration.AddInMemoryCollection(
    DotEnvLoader.GetRazorpayConfiguration()
        .Where(pair => !string.IsNullOrWhiteSpace(pair.Value)));


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
builder.Services.AddSingleton<IPanchangEclipseService, PanchangEclipseService>();
builder.Services.Configure<LocationProviderOptions>(builder.Configuration.GetSection("LocationProvider"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ILocationSearchProvider, NominatimLocationSearchProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocationProviderOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddScoped<ILocationSearchService, LocationSearchService>();
builder.Services.AddSingleton<AspectService>();
builder.Services.AddSingleton<PlanetStrengthService>();
builder.Services.AddSingleton<OverallKundaliScoreService>();
builder.Services.AddSingleton<RemediesRecommendationService>();
builder.Services.AddSingleton<CompatibilityScoreService>();
builder.Services.AddSingleton<CompatibilityDoshaAnalysisService>();
builder.Services.AddSingleton<SudarshanChakraService>();
builder.Services.AddSingleton<DivisionalChartService>();
builder.Services.AddSingleton<YogaDoshaActivationService>();
builder.Services.AddSingleton<AdvancedStrengthTableService>();
builder.Services.AddSingleton<AstrologyKnowledgeService>();
builder.Services.AddSingleton<DivisionalChartMetadataService>();
builder.Services.AddScoped<LifeAreaAnalysisService>();
builder.Services.AddHttpClient<LifeAreaInsightService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DhruvaNarrationOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
});
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
builder.Services.AddSingleton<IMuhurthaEclipseWindowService, MuhurthaEclipseWindowService>();
builder.Services.AddSingleton<IVastuMuhurthaEligibilityService, VastuMuhurthaEligibilityService>();
builder.Services.AddSingleton<IMuhurthaRuleEvaluationService, MuhurthaRuleEvaluationService>();
builder.Services.AddSingleton<IMuhurthaCandidateGenerator, MuhurthaCandidateGenerator>();
builder.Services.AddSingleton<IMuhurthaScoringService, MuhurthaScoringService>();
builder.Services.AddSingleton<IMuhurthaRecommendationService, MuhurthaRecommendationService>();
builder.Services.AddSingleton<IMuhurthaService, MuhurthaService>();
builder.Services.AddSingleton<IGrahaPraveshaProfileProvider>(_ =>
    new GrahaPraveshaProfileProvider(Path.Combine(AppContext.BaseDirectory, "Data", "Muhurtha")));
builder.Services.AddSingleton<IPanchangSnapshotReader, EphemerisPanchangSnapshotReader>();
builder.Services.AddSingleton<GrahaPraveshaRuleEngine>();
builder.Services.AddSingleton<IChakraRuleEvaluator, KalashaChakraEvaluator>();
builder.Services.AddSingleton<IChakraRuleEvaluator, VrishaChakraEvaluator>();
builder.Services.AddSingleton<IGrahaPraveshaBirthChartEvaluator, ChartGrahaPraveshaBirthEvaluator>();
builder.Services.AddSingleton<IGrahaPraveshaMuhurthaService, GrahaPraveshaMuhurthaService>();
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
builder.Services.AddScoped<HoroscopeNatalScorer>();
builder.Services.AddScoped<HoroscopeHorizonExtrasBuilder>();
builder.Services.AddScoped<IHoroscopePeriodService, HoroscopePeriodService>();
builder.Services.AddSingleton<IHoroscopeRashifalService, HoroscopeRashifalService>();
builder.Services.AddScoped<IRetrogradeTransitService, RetrogradeTransitService>();
builder.Services.Configure<ReportShareSmtpOptions>(builder.Configuration.GetSection("ReportShare:Smtp"));
builder.Services.Configure<SmsOtpOptions>(builder.Configuration.GetSection("TwoFactorSms"));
builder.Services.AddSingleton<IReportHtmlTemplateService, ReportHtmlTemplateService>();
builder.Services.AddSingleton<ReportLifeChapterBuilder>();
builder.Services.AddSingleton<IHtmlReportPdfRenderer, ChromeHtmlReportPdfRenderer>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<ISmsOtpSender, TwoFactorSmsOtpSender>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmsOtpOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});
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
builder.Services.AddScoped<IStoreHandoffService, StoreHandoffService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.Configure<SubscriptionOptions>(builder.Configuration.GetSection("Subscriptions"));
builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
builder.Services.PostConfigure<RazorpayOptions>(options =>
{
    var overlay = DotEnvLoader.GetRazorpayConfiguration();
    if (!string.IsNullOrWhiteSpace(overlay["Razorpay:KeyId"]))
    {
        options.KeyId = overlay["Razorpay:KeyId"]!;
    }

    if (!string.IsNullOrWhiteSpace(overlay["Razorpay:KeySecret"]))
    {
        options.KeySecret = overlay["Razorpay:KeySecret"]!;
    }

    if (!string.IsNullOrWhiteSpace(overlay["Razorpay:WebhookSecret"]))
    {
        options.WebhookSecret = overlay["Razorpay:WebhookSecret"]!;
    }
});
builder.Services.Configure<DhruvaNarrationOptions>(builder.Configuration.GetSection("DhruvaNarration"));
builder.Services.PostConfigure<DhruvaNarrationOptions>(options =>
{
    var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        options.ApiKey = apiKey;
    }
});
builder.Services.AddHttpClient<IDhruvaNarrationService, DhruvaNarrationService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DhruvaNarrationOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
});
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("DhruvaNarration", TimeSpan.FromSeconds(180));
});
builder.Services.AddHttpClient<IRazorpayClient, RazorpayClient>(client =>
{
    client.BaseAddress = new Uri("https://api.razorpay.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<PlanCatalogCache>();
builder.Services.AddScoped<IPlanCatalog, PlanCatalogService>();
builder.Services.AddScoped<ICouponOfferService, CouponOfferService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<IDhruvaUsageService, DhruvaUsageService>();
builder.Services.AddScoped<IDhruvaChatHistoryService, DhruvaChatHistoryService>();
builder.Services.AddScoped<IUserAnalysisService, UserAnalysisService>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<PredictionContextMapper>();

builder.Services.AddSingleton(_ => PredictionEngineFactory.CreateDefault());
builder.Services.AddScoped<NumerologyCalculator>();
builder.Services.AddSingleton<NumerologyMetadataService>();

builder.Services.AddStoreApplication();
builder.Services.AddStoreInfrastructure(builder.Configuration);
builder.Services.Configure<HoraLife.Store.Application.Options.StoreInventoryOptions>(
    builder.Configuration.GetSection(HoraLife.Store.Application.Options.StoreInventoryOptions.SectionName));
builder.Services.AddHostedService<KundaliEngine.Api.Services.Store.StoreInventoryReservationExpiryHostedService>();


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
    options.AddPolicy("GoogleAuth", httpContext =>
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
app.UseMiddleware<LastSeenMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapControllers();

await EnsureUserSubscriptionsTableAsync(app);
await EnsureKundaliFamilyColumnsAsync(app);
await EnsureDhruvaDailyUsageTableAsync(app);
await EnsureDhruvaQuestionLogTableAsync(app);
await EnsureDhruvaChatMessagesTableAsync(app);
await EnsureUserPracticeProfileColumnAsync(app);
await EnsureUserGoogleSubjectColumnAsync(app);
await EnsureUserProfileImageColumnAsync(app);
await EnsureUserLastSeenColumnAsync(app);
await EnsureProductFeedbackTableAsync(app);
await EnsureBillingPlanDhruvaColumnsAsync(app);

app.Run();

static async Task EnsureUserSubscriptionsTableAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS public."UserSubscriptions" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "PlanCode" character varying(50) NOT NULL,
            "BillingCycle" character varying(20) NOT NULL,
            "Status" character varying(40) NOT NULL,
            "RazorpaySubscriptionId" character varying(80) NULL,
            "RazorpayOrderId" character varying(80) NULL,
            "RazorpayPaymentId" character varying(80) NULL,
            "CurrentPeriodEnd" timestamp with time zone NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            CONSTRAINT "PK_UserSubscriptions" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_UserSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_UserId" ON public."UserSubscriptions" ("UserId");
        CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_RazorpaySubscriptionId" ON public."UserSubscriptions" ("RazorpaySubscriptionId");
        CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_RazorpayOrderId" ON public."UserSubscriptions" ("RazorpayOrderId");
        """);
}

static async Task EnsureDhruvaDailyUsageTableAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS public."DhruvaDailyUsages" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "UsageDate" date NOT NULL,
            "LifeQuestionCount" integer NOT NULL,
            CONSTRAINT "PK_DhruvaDailyUsages" PRIMARY KEY ("Id")
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_DhruvaDailyUsages_UserId_UsageDate"
            ON public."DhruvaDailyUsages" ("UserId", "UsageDate");
        """);
}

static async Task EnsureDhruvaQuestionLogTableAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS public."DhruvaQuestionLogs" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "AskedAt" timestamp with time zone NOT NULL,
            "IsAiMode" boolean NOT NULL,
            "Kind" character varying(40) NOT NULL,
            "Language" character varying(20) NOT NULL,
            "PromptTokens" integer NOT NULL,
            "CompletionTokens" integer NOT NULL,
            "TotalTokens" integer NOT NULL,
            CONSTRAINT "PK_DhruvaQuestionLogs" PRIMARY KEY ("Id")
        );
        CREATE INDEX IF NOT EXISTS "IX_DhruvaQuestionLogs_UserId"
            ON public."DhruvaQuestionLogs" ("UserId");
        """);
}

static async Task EnsureDhruvaChatMessagesTableAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS public."DhruvaChatMessages" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "KundaliId" uuid NOT NULL,
            "Question" character varying(2000) NOT NULL,
            "Response" text NOT NULL,
            "AskedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_DhruvaChatMessages" PRIMARY KEY ("Id")
        );
        CREATE INDEX IF NOT EXISTS "IX_DhruvaChatMessages_UserId_KundaliId_AskedAt"
            ON public."DhruvaChatMessages" ("UserId", "KundaliId", "AskedAt");
        """);
}

static async Task EnsureProductFeedbackTableAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS public."ProductFeedback" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "Surface" character varying(40) NOT NULL,
            "ContextKey" character varying(200) NOT NULL,
            "Sentiment" character varying(20) NOT NULL,
            "Topic" character varying(40) NULL,
            "Note" character varying(400) NULL,
            "PlanCode" character varying(50) NOT NULL,
            "FeedbackDate" date NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_ProductFeedback" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_ProductFeedback_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductFeedback_User_Surface_Context_Date"
            ON public."ProductFeedback" ("UserId", "Surface", "ContextKey", "FeedbackDate");
        CREATE INDEX IF NOT EXISTS "IX_ProductFeedback_CreatedAt"
            ON public."ProductFeedback" ("CreatedAt");
        """);
}

static async Task EnsureUserPracticeProfileColumnAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "PracticeProfile" text;
        """);
}

static async Task EnsureUserLastSeenColumnAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "LastSeenAt" timestamp with time zone;
        """);
}

static async Task EnsureUserProfileImageColumnAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "ProfileImageUrl" character varying(1000);
        """);
}

static async Task EnsureUserGoogleSubjectColumnAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "GoogleSubject" character varying(128);
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_GoogleSubject"
            ON public."Users" ("GoogleSubject")
            WHERE "GoogleSubject" IS NOT NULL;
        """);
}

static async Task EnsureBillingPlanDhruvaColumnsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'BillingPlanConfigs'
                  AND column_name = 'AiChatEnabled'
            ) THEN
                ALTER TABLE public."BillingPlanConfigs" ADD COLUMN "AiChatEnabled" boolean NOT NULL DEFAULT false;
                ALTER TABLE public."BillingPlanConfigs" ADD COLUMN "LifeQuestionLimit" integer NULL;
                UPDATE public."BillingPlanConfigs"
                SET "AiChatEnabled" = ("Code" <> 'Free'),
                    "LifeQuestionLimit" = CASE WHEN "Code" = 'Free' THEN 2 ELSE NULL END;
            END IF;
        END $$;
        """);
}

static async Task EnsureKundaliFamilyColumnsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE public."Kundalis" ADD COLUMN IF NOT EXISTS "Surname" character varying(150);
        ALTER TABLE public."Kundalis" ADD COLUMN IF NOT EXISTS "FatherName" character varying(150);
        ALTER TABLE public."Kundalis" ADD COLUMN IF NOT EXISTS "MotherName" character varying(150);
        ALTER TABLE public."Kundalis" ADD COLUMN IF NOT EXISTS "Gotra" character varying(100);
        """);
}








