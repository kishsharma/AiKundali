using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.KnowledgeBase.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register core astrology services for DI (Phase 1 foundation)
builder.Services.AddSingleton<ChartService>();
builder.Services.AddSingleton<PlanetScoreService>();
builder.Services.AddSingleton<HouseScoreService>();
builder.Services.AddSingleton<DashaService>();
builder.Services.AddSingleton<PanchangService>();
builder.Services.AddSingleton<AspectService>();
builder.Services.AddSingleton<PlanetStrengthService>();
builder.Services.AddSingleton<OverallKundaliScoreService>();
builder.Services.AddSingleton<CompatibilityScoreService>();
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
builder.Services.AddSingleton(_ =>
{
    var yogaMetadataPath = Path.Combine(AppContext.BaseDirectory, "Data", "KundaliYoga", "Yogas.json");
    return new YogaMetadataService(yogaMetadataPath);
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
