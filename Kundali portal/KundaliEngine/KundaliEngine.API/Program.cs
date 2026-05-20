using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Astrology.Services.Compatibility;

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
