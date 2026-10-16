using KundaliEngine.API.DTOs.Responses.Numerology;

namespace HoraLife.PredictionEngine.Tests;

public class NumerologyCalculatorTests
{
    [Fact]
    public void Calculate_BuildsLoShuAndPersonalYearsFromTheDate()
    {
        var report = new NumerologyCalculator().Calculate(new DateOnly(1990, 5, 14), 2026);

        Assert.Equal(5, report.Mulank);
        Assert.Equal(2, report.Bhagyank);
        Assert.Equal(new[] { 2, 3, 6, 7, 8 }, report.LoShu.Missing);
        Assert.Contains(report.LoShu.Repeated, item => item.Number == 1 && item.Count == 2);
        Assert.Contains(report.LoShu.Repeated, item => item.Number == 9 && item.Count == 2);
        Assert.Equal(new[] { 4, 9, 2, 3, 5, 7, 8, 1, 6 }, report.LoShu.Cells.Select(cell => cell.Number));
        Assert.Equal(2, report.LoShu.Cells.Single(cell => cell.Number == 1).Count);
        Assert.Equal(0, report.LoShu.Cells.Single(cell => cell.Number == 6).Count);

        Assert.Equal(2026, report.PersonalYear.Year);
        Assert.Equal(2, report.PersonalYear.Number);
        Assert.Equal("Moon", report.PersonalYear.Planet);
        Assert.Equal(2027, report.NextPersonalYear.Year);
        Assert.Equal(3, report.NextPersonalYear.Number);
        Assert.Equal("Jupiter", report.NextPersonalYear.Planet);
    }

    [Fact]
    public void CalculateName_ReducesKishoreToSeven()
    {
        var calculator = new NumerologyCalculator();

        Assert.True(calculator.TryCalculateName("Kishore", out var name));
        Assert.NotNull(name);
        Assert.Equal(25, name!.Compound);
        Assert.Equal(7, name.Number);
        Assert.Equal("Ketu", name.Planet);
        Assert.Contains("K2", name.Calculation);

        Assert.True(calculator.TryCalculateName("   ", out var empty));
        Assert.Null(empty);
        Assert.False(calculator.TryCalculateName("123", out _));
    }

    [Fact]
    public void CalculateMobile_ReducesDigitsAndSuggestsLastDigitChange()
    {
        var calculator = new NumerologyCalculator();

        Assert.True(calculator.TryCalculateMobile("+91 98765 43210", out var mobile));
        Assert.NotNull(mobile);
        Assert.Equal(55, mobile!.Compound);
        Assert.Equal(1, mobile.Number);
        Assert.Equal("Sun", mobile.Planet);
        Assert.Contains("9 + 8 + 7", mobile.Calculation);

        var suggestions = calculator.SuggestMobileCorrections(mobile, [2, 5]);
        Assert.Contains(suggestions, item => item.Contains("Change the last digit") && item.Contains("reduces to 2"));
        Assert.True(calculator.TryCalculateMobile("   ", out var blank));
        Assert.Null(blank);
        Assert.False(calculator.TryCalculateMobile("abc", out _));
    }

    [Fact]
    public void SuggestNameCorrections_AddsLetterTowardSupportiveNumber()
    {
        var calculator = new NumerologyCalculator();
        Assert.True(calculator.TryCalculateName("HoraLife", out var business));
        Assert.NotNull(business);

        var suggestions = calculator.SuggestNameCorrections(business!, [1, 5]);
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, item => item.Contains("Add a letter valued") && item.Contains("HoraLife"));
    }
}
