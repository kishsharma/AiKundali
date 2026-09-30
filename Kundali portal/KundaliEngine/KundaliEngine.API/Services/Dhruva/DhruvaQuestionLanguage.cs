namespace KundaliEngine.API.Services.Dhruva;

public static class DhruvaQuestionLanguage
{
    public const string Hindi = "hindi";
    public const string English = "english";
    public const string Other = "other";

    public static string Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return English;
        }

        var hindi = 0;
        var other = 0;
        var latin = 0;
        foreach (var character in text)
        {
            if (character is >= '\u0900' and <= '\u097F')
            {
                hindi++;
            }
            else if (char.IsLetter(character) && character > 127)
            {
                other++;
            }
            else if (char.IsLetter(character))
            {
                latin++;
            }
        }

        if (hindi > 0 && hindi >= other)
        {
            return Hindi;
        }

        if (other > latin)
        {
            return Other;
        }

        return English;
    }
}
