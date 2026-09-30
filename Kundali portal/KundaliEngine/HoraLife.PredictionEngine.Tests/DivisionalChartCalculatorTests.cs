using KundaliEngine.Astrology.Services.DivisionalCharts.Calculators;
using KundaliEngine.Domain.Enums;

namespace HoraLife.PredictionEngine.Tests;

public class DivisionalChartCalculatorTests
{
    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Aries, 7.5, ZodiacSign.Taurus)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Leo)]
    [InlineData(ZodiacSign.Gemini, 0, ZodiacSign.Sagittarius)]
    public void D4_UsesMovableFixedAndDualStarts(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new ChaturthamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Aries, 1.875, ZodiacSign.Taurus)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Leo)]
    public void D16_StartsFromAriesOrLeo(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new ShodasamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Sagittarius)]
    public void D20_StartsFromAriesOrSagittarius(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new VimsamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Leo)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Cancer)]
    public void D24_StartsFromLeoOrCancer(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new SiddhamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Cancer)]
    [InlineData(ZodiacSign.Gemini, 0, ZodiacSign.Libra)]
    [InlineData(ZodiacSign.Cancer, 0, ZodiacSign.Capricorn)]
    public void D27_FollowsElementStarts(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new BhamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Aries, 4.99, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Aries, 5, ZodiacSign.Aquarius)]
    [InlineData(ZodiacSign.Aries, 9.99, ZodiacSign.Aquarius)]
    [InlineData(ZodiacSign.Aries, 10, ZodiacSign.Sagittarius)]
    [InlineData(ZodiacSign.Aries, 17.99, ZodiacSign.Sagittarius)]
    [InlineData(ZodiacSign.Aries, 18, ZodiacSign.Gemini)]
    [InlineData(ZodiacSign.Aries, 24.99, ZodiacSign.Gemini)]
    [InlineData(ZodiacSign.Aries, 25, ZodiacSign.Libra)]
    [InlineData(ZodiacSign.Aries, 29.99, ZodiacSign.Libra)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Taurus)]
    [InlineData(ZodiacSign.Taurus, 4.99, ZodiacSign.Taurus)]
    [InlineData(ZodiacSign.Taurus, 5, ZodiacSign.Virgo)]
    [InlineData(ZodiacSign.Taurus, 11.99, ZodiacSign.Virgo)]
    [InlineData(ZodiacSign.Taurus, 12, ZodiacSign.Pisces)]
    [InlineData(ZodiacSign.Taurus, 19.99, ZodiacSign.Pisces)]
    [InlineData(ZodiacSign.Taurus, 20, ZodiacSign.Capricorn)]
    [InlineData(ZodiacSign.Taurus, 24.99, ZodiacSign.Capricorn)]
    [InlineData(ZodiacSign.Taurus, 25, ZodiacSign.Scorpio)]
    [InlineData(ZodiacSign.Taurus, 29.99, ZodiacSign.Scorpio)]
    public void D30_UsesUnevenParasharaBoundaries(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new TrimsamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Libra)]
    public void D40_StartsFromAriesOrLibra(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new KhavedamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Leo)]
    [InlineData(ZodiacSign.Gemini, 0, ZodiacSign.Sagittarius)]
    public void D45_UsesMovableFixedAndDualStarts(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new AkshavedamsaCalculator().CalculateSign(sign, degree));
    }

    [Theory]
    [InlineData(ZodiacSign.Aries, 0, ZodiacSign.Aries)]
    [InlineData(ZodiacSign.Aries, 0.5, ZodiacSign.Taurus)]
    [InlineData(ZodiacSign.Taurus, 0, ZodiacSign.Scorpio)]
    public void D60_StartsFromTheSignOrItsSeventh(ZodiacSign sign, double degree, ZodiacSign expected)
    {
        Assert.Equal(expected, new ShashtiamsaCalculator().CalculateSign(sign, degree));
    }
}
