using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Infrastructure;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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
builder.Services.AddSingleton<CompatibilityScoreService>();
builder.Services.AddSingleton<CompatibilityDoshaAnalysisService>();
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is missing in appsettings.json");
}

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

builder.Services.AddAuthorization();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
