using System.Text.Json;
using KundaliEngine.API.DTOs.Requests;

namespace HoraLife.PredictionEngine.Tests;

public class BirthTimeZoneRequestTests
{
    [Fact]
    public void GenerateKundaliRequest_BindsIndiaOffsetAndDefaultsToIst()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var explicitOffset = JsonSerializer.Deserialize<GenerateKundaliRequest>("""{"timeZoneOffset":5.5}""", options);
        var defaultOffset = JsonSerializer.Deserialize<GenerateKundaliRequest>("{}", options);

        Assert.NotNull(explicitOffset);
        Assert.NotNull(defaultOffset);
        Assert.Equal(5.5, explicitOffset.TimeZoneOffset);
        Assert.Equal(5.5, defaultOffset.TimeZoneOffset);
    }
}
