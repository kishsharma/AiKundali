using System.Globalization;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.LifeArea;

internal static class LifeAreaCopy
{
    private static readonly string[] AreaKeys =
    [
        "personality", "health", "wealth", "career", "business", "marriage", "children",
        "property", "education", "parents", "vehicles", "spirituality", "strengthAndResilience",
        "adversity", "ancestry"
    ];

    public static string Locale(string? locale) => locale is "hi" or "kn" ? locale : "en";

    public static string AreaName(string key, string locale)
    {
        var index = Array.IndexOf(AreaKeys, key);
        if (index < 0)
        {
            return key;
        }

        var table = locale switch
        {
            "hi" => new[]
            {
                "व्यक्तित्व", "स्वास्थ्य", "धन", "करियर", "व्यवसाय", "विवाह", "संतान",
                "संपत्ति", "शिक्षा", "माता-पिता", "वाहन", "आध्यात्मिकता", "शक्ति और लचीलापन",
                "प्रतिकूलता", "वंश"
            },
            "kn" => new[]
            {
                "ವ್ಯಕ್ತಿತ್ವ", "ಆರೋಗ್ಯ", "ಸಂಪತ್ತು", "ವೃತ್ತಿ", "ವ್ಯವಹಾರ", "ವಿವಾಹ", "ಮಕ್ಕಳು",
                "ಆಸ್ತಿ", "ಶಿಕ್ಷಣ", "ಪೋಷಕರು", "ವಾಹನಗಳು", "ಆಧ್ಯಾತ್ಮ", "ಶಕ್ತಿ ಮತ್ತು ಸ್ಥೈರ್ಯ",
                "ಪ್ರತಿಕೂಲತೆ", "ವಂಶ"
            },
            _ => new[]
            {
                "Personality", "Health", "Wealth", "Career", "Business", "Marriage", "Children",
                "Property", "Education", "Parents", "Vehicles", "Spirituality", "Strength and resilience",
                "Adversity", "Ancestry"
            }
        };

        return table[index];
    }

    public static string PlanetName(Planet planet, string locale)
    {
        var index = planet switch
        {
            Planet.Sun => 0,
            Planet.Moon => 1,
            Planet.Mars => 2,
            Planet.Mercury => 3,
            Planet.Jupiter => 4,
            Planet.Venus => 5,
            Planet.Saturn => 6,
            Planet.Rahu => 7,
            _ => 8
        };
        var table = locale switch
        {
            "hi" => new[] { "सूर्य", "चंद्र", "मंगल", "बुध", "गुरु", "शुक्र", "शनि", "राहु", "केतु" },
            "kn" => new[] { "ಸೂರ್ಯ", "ಚಂದ್ರ", "ಮಂಗಳ", "ಬುಧ", "ಗುರು", "ಶುಕ್ರ", "ಶನಿ", "ರಾಹು", "ಕೇತು" },
            _ => new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" }
        };
        return table[index];
    }

    public static string SignName(ZodiacSign sign, string locale)
    {
        var index = Math.Clamp((int)sign - 1, 0, 11);
        var table = locale switch
        {
            "hi" => new[] { "मेष", "वृषभ", "मिथुन", "कर्क", "सिंह", "कन्या", "तुला", "वृश्चिक", "धनु", "मकर", "कुंभ", "मीन" },
            "kn" => new[] { "ಮೇಷ", "ವೃಷಭ", "ಮಿಥುನ", "ಕರ್ಕ", "ಸಿಂಹ", "ಕನ್ಯಾ", "ತುಲಾ", "ವೃಶ್ಚಿಕ", "ಧನು", "ಮಕರ", "ಕುಂಭ", "ಮೀನ" },
            _ => new[] { "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo", "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces" }
        };
        return table[index];
    }

    public static string Dignity(string key, string locale)
    {
        var index = key switch
        {
            "exalted" => 0,
            "own" => 1,
            "moolatrikona" => 2,
            "friend" => 3,
            "enemy" => 4,
            "debilitated" => 5,
            _ => 6
        };
        var table = locale switch
        {
            "hi" => new[] { "उच्च", "स्वराशि", "मूलत्रिकोण", "मित्र राशि", "शत्रु राशि", "नीच", "सम राशि" },
            "kn" => new[] { "ಉಚ್ಚ", "ಸ್ವಕ್ಷೇತ್ರ", "ಮೂಲತ್ರಿಕೋಣ", "ಮಿತ್ರ ರಾಶಿ", "ಶತ್ರು ರಾಶಿ", "ನೀಚ", "ಸಮ ರಾಶಿ" },
            _ => new[] { "exalted", "own sign", "moolatrikona", "friendly sign", "enemy sign", "debilitated", "neutral sign" }
        };
        return table[index];
    }

    public static string VerdictText(string verdict, string areaName, string locale) => (verdict, locale) switch
    {
        ("supported", "hi") => $"{areaName} के लिए देखे गए चार्ट सहायक झुकाव दिखाते हैं।",
        ("supported", "kn") => $"{areaName}ಗಾಗಿ ನೋಡಿದ ಚಾರ್ಟ್‌ಗಳು ಬೆಂಬಲದ ಒಲವು ತೋರಿಸುತ್ತವೆ.",
        ("supported", _) => $"The charts consulted for {areaName} lean supportive.",
        ("needsCare", "hi") => $"{areaName} के लिए देखे गए चार्ट सावधानी माँगते हैं।",
        ("needsCare", "kn") => $"{areaName}ಗಾಗಿ ನೋಡಿದ ಚಾರ್ಟ್‌ಗಳು ಎಚ್ಚರಿಕೆಯನ್ನು ಕೇಳುತ್ತವೆ.",
        ("needsCare", _) => $"The charts consulted for {areaName} need care.",
        (_, "hi") => $"{areaName} के लिए देखे गए चार्ट मिश्रित संकेत देते हैं।",
        (_, "kn") => $"{areaName}ಗಾಗಿ ನೋಡಿದ ಚಾರ್ಟ್‌ಗಳು ಮಿಶ್ರ ಸೂಚನೆ ನೀಡುತ್ತವೆ.",
        _ => $"The charts consulted for {areaName} are mixed."
    };

    public static string FactorReading(
        string planet,
        string natalSign,
        string natalDignity,
        string specialistChart,
        string specialistSign,
        string specialistDignity,
        bool retrograde,
        string locale)
    {
        var line = locale switch
        {
            "hi" => $"{planet} जन्म कुंडली में {natalSign} ({natalDignity}) में है, और {specialistChart} में {specialistSign} ({specialistDignity}) में है।",
            "kn" => $"{planet} ಜನ್ಮ ಕುಂಡಲಿಯಲ್ಲಿ {natalSign} ({natalDignity}) ದಲ್ಲಿದೆ, ಮತ್ತು {specialistChart} ನಲ್ಲಿ {specialistSign} ({specialistDignity}) ದಲ್ಲಿದೆ.",
            _ => $"{planet} is in {natalSign} ({natalDignity}) in the birth chart, and in {specialistSign} ({specialistDignity}) in {specialistChart}."
        };

        if (!retrograde)
        {
            return line;
        }

        return locale switch
        {
            "hi" => line + " जन्म कुंडली में यह वक्री है।",
            "kn" => line + " ಜನ್ಮ ಕುಂಡಲಿಯಲ್ಲಿ ಇದು ವಕ್ರಿಯಾಗಿದೆ.",
            _ => line + " It is retrograde in the birth chart."
        };
    }

    public static string CrossValidation(IReadOnlyList<string> agree, IReadOnlyList<string> differ, string specialist, string locale)
    {
        if (specialist == "D1" || (agree.Count == 0 && differ.Count == 0))
        {
            return locale switch
            {
                "hi" => "यह पढ़ना जन्म कुंडली पर टिका है।",
                "kn" => "ಈ ಓದು ಜನ್ಮ ಕುಂಡಲಿಯ ಮೇಲೆ ನಿಂತಿದೆ.",
                _ => "This reading rests on the birth chart."
            };
        }

        var agreeNames = JoinNames(agree, locale);
        var differNames = JoinNames(differ, locale);
        var agreeLine = agree.Count switch
        {
            0 => null,
            1 => locale switch
            {
                "hi" => $"{agreeNames} D1 और {specialist} में सहमत है।",
                "kn" => $"{agreeNames} D1 ಮತ್ತು {specialist} ನಲ್ಲಿ ಒಪ್ಪುತ್ತದೆ.",
                _ => $"{agreeNames} agrees between D1 and {specialist}."
            },
            _ => locale switch
            {
                "hi" => $"{agreeNames} D1 और {specialist} में सहमत हैं।",
                "kn" => $"{agreeNames} D1 ಮತ್ತು {specialist} ನಲ್ಲಿ ಒಪ್ಪುತ್ತವೆ.",
                _ => $"{agreeNames} agree between D1 and {specialist}."
            }
        };
        var differLine = differ.Count switch
        {
            0 => null,
            1 => locale switch
            {
                "hi" => $"{differNames} D1 और {specialist} में भिन्न है।",
                "kn" => $"{differNames} D1 ಮತ್ತು {specialist} ನಲ್ಲಿ ಭಿನ್ನವಾಗಿದೆ.",
                _ => $"{differNames} differs between D1 and {specialist}."
            },
            _ => locale switch
            {
                "hi" => $"{differNames} D1 और {specialist} में भिन्न हैं।",
                "kn" => $"{differNames} D1 ಮತ್ತು {specialist} ನಲ್ಲಿ ಭಿನ್ನವಾಗಿವೆ.",
                _ => $"{differNames} differ between D1 and {specialist}."
            }
        };

        return string.Join(" ", new[] { agreeLine, differLine }.Where(line => line is not null));
    }

    private static string JoinNames(IReadOnlyList<string> names, string locale)
    {
        var conjunction = locale switch
        {
            "hi" => "और",
            "kn" => "ಮತ್ತು",
            _ => "and"
        };
        if (names.Count <= 1)
        {
            return names.Count == 0 ? string.Empty : names[0];
        }

        if (names.Count == 2)
        {
            return $"{names[0]} {conjunction} {names[1]}";
        }

        return string.Join(", ", names.Take(names.Count - 1)) + $" {conjunction} {names[^1]}";
    }

    public static string Timing(string? mahadasha, string? antardasha, bool connected, string locale)
    {
        if (string.IsNullOrWhiteSpace(mahadasha) || string.IsNullOrWhiteSpace(antardasha))
        {
            return locale switch
            {
                "hi" => "इस कुंडली के लिए वर्तमान दशा उपलब्ध नहीं है।",
                "kn" => "ಈ ಕುಂಡಲಿಗೆ ಪ್ರಸ್ತುತ ದಶೆ ಲಭ್ಯವಿಲ್ಲ.",
                _ => "Current dasha was not available for this chart."
            };
        }

        if (connected)
        {
            return locale switch
            {
                "hi" => $"{mahadasha} महादशा और {antardasha} अंतरदशा इस क्षेत्र से जुड़ती हैं।",
                "kn" => $"{mahadasha} ಮಹಾದಶೆ ಮತ್ತು {antardasha} ಅಂತರ್ದಶೆ ಈ ಕ್ಷೇತ್ರಕ್ಕೆ ಸೇರುತ್ತವೆ.",
                _ => $"{mahadasha} mahadasha and {antardasha} antardasha connect with this area."
            };
        }

        return locale switch
        {
            "hi" => $"{mahadasha} महादशा और {antardasha} अंतरदशा इस क्षेत्र की पृष्ठभूमि में हैं।",
            "kn" => $"{mahadasha} ಮಹಾದಶೆ ಮತ್ತು {antardasha} ಅಂತರ್ದಶೆ ಈ ಕ್ಷೇತ್ರದ ಹಿನ್ನೆಲೆಯಲ್ಲಿವೆ.",
            _ => $"{mahadasha} mahadasha and {antardasha} antardasha sit in the background of this area."
        };
    }

    public static string MonthSpan(DateTime start, DateTime end, string locale)
    {
        var culture = locale switch
        {
            "hi" => CultureInfo.GetCultureInfo("hi-IN"),
            "kn" => CultureInfo.GetCultureInfo("kn-IN"),
            _ => CultureInfo.GetCultureInfo("en-US")
        };
        var shownEnd = end.Date.AddDays(-1);
        if (shownEnd < start.Date)
        {
            shownEnd = start.Date;
        }

        var startLabel = start.ToString("MMM yyyy", culture);
        var endLabel = shownEnd.ToString("MMM yyyy", culture);
        return startLabel == endLabel ? startLabel : $"{startLabel} – {endLabel}";
    }

    public static string PeriodReading(string planet, string label, string tone, string locale) => (tone, locale) switch
    {
        ("support", "hi") => $"{label} में {planet} इस क्षेत्र के लिए सहायक झुकाव रखता है।",
        ("support", "kn") => $"{label} ನಲ್ಲಿ {planet} ಈ ಕ್ಷೇತ್ರಕ್ಕೆ ಸಹಾಯಕ ಒಲವು ಇಡುತ್ತದೆ.",
        ("support", _) => $"{planet} in {label} leans helpful for this area.",
        ("care", "hi") => $"{label} में {planet} इस क्षेत्र में थोड़ी अतिरिक्त सावधानी माँगता है।",
        ("care", "kn") => $"{label} ನಲ್ಲಿ {planet} ಈ ಕ್ಷೇತ್ರದಲ್ಲಿ ಸ್ವಲ್ಪ ಹೆಚ್ಚು ಎಚ್ಚರಿಕೆ ಕೇಳುತ್ತದೆ.",
        ("care", _) => $"{planet} in {label} asks for a little extra care.",
        (_, "hi") => $"{label} में {planet} इस क्षेत्र को छूता है, उस पर कब्जा नहीं करता।",
        (_, "kn") => $"{label} ನಲ್ಲಿ {planet} ಈ ಕ್ಷೇತ್ರವನ್ನು ಮುಟ್ಟುತ್ತದೆ, ಅದನ್ನು ಆವರಿಸುವುದಿಲ್ಲ.",
        _ => $"{planet} in {label} touches this area without taking it over."
    };

    public static string? Note(string? metadataNote, bool usesD60, string locale)
    {
        string? health = null;
        if (!string.IsNullOrWhiteSpace(metadataNote))
        {
            health = locale switch
            {
                "hi" => "यह पारंपरिक ज्योतिषीय व्याख्या है, चिकित्सकीय निदान नहीं।",
                "kn" => "ಇದು ಸಾಂಪ್ರದಾಯಿಕ ಜ್ಯೋತಿಷ್ಯ ವ್ಯಾಖ್ಯಾನ, ವೈದ್ಯಕೀಯ ರೋಗನಿರ್ಣಯವಲ್ಲ.",
                _ => "This is a traditional astrological interpretation, not a medical diagnosis."
            };
        }

        string? time = usesD60
            ? locale switch
            {
                "hi" => "D60 जन्म समय के प्रति बहुत संवेदनशील है। इसे स्वतंत्र निर्णय नहीं, एक संशोधक मानें।",
                "kn" => "D60 ಜನ್ಮ ಸಮಯಕ್ಕೆ ಬಹಳ ಸೂಕ್ಷ್ಮ. ಇದನ್ನು ಸ್ವತಂತ್ರ ತೀರ್ಮಾನವಲ್ಲ, ಒಂದು ತಿದ್ದುಪಡಿ ಎಂದು ತೆಗೆದುಕೊಳ್ಳಿ.",
                _ => "D60 is very sensitive to birth time. Treat it as a modifier, not a standalone judgment."
            }
            : null;

        if (health is null)
        {
            return time;
        }

        return time is null ? health : $"{health} {time}";
    }

    public static string PlainMeaning(
        string areaKey,
        string verdict,
        string specialist,
        int agree,
        int differ,
        bool timingAvailable,
        bool connected,
        string locale)
    {
        var topic = Topic(areaKey, locale);
        var outcome = Outcome(verdict, locale);
        var charts = Charts(specialist, agree, differ, locale);
        var period = Period(timingAvailable, connected, locale);
        return $"{topic} {outcome} {charts} {period}";
    }

    private static string Topic(string areaKey, string locale)
    {
        var index = Array.IndexOf(AreaKeys, areaKey);
        if (index < 0)
        {
            index = 0;
        }

        var table = locale switch
        {
            "hi" => new[]
            {
                "यह इस बारे में है कि आप कैसे हैं और लोग आपको कैसे देखते हैं।",
                "यह आपकी ऊर्जा और शरीर के टिकने के बारे में है।",
                "यह पैसा आने और उसके साथ बने रहने के बारे में है।",
                "यह आपके काम और बनती पहचान के बारे में है।",
                "यह व्यापार, सौदों और अपनी कमाई के बारे में है।",
                "यह साथी और वैवाहिक जीवन के बारे में है।",
                "यह संतान और उनसे आपके रिश्ते के बारे में है।",
                "यह घर, जमीन और आपके पास की चीज़ों के बारे में है।",
                "यह पढ़ाई और सीखने के बारे में है।",
                "यह माता-पिता के साथ आपके रिश्ते के बारे में है।",
                "यह आराम, यात्रा और वाहनों के बारे में है।",
                "यह आस्था और भीतर की खोज के बारे में है।",
                "यह इस बारे में है कि मुश्किल आने पर आप कितनी जल्दी संभलते हैं।",
                "यह जीवन में आने वाली रुकावटों के बारे में है।",
                "यह परिवार की जड़ों और पुरानी पीढ़ियों से आए हुए के बारे में है।"
            },
            "kn" => new[]
            {
                "ಇದು ನೀವು ಹೇಗಿದ್ದೀರಿ ಮತ್ತು ಜನರು ನಿಮ್ಮನ್ನು ಹೇಗೆ ನೋಡುತ್ತಾರೆ ಎಂಬುದರ ಬಗ್ಗೆ.",
                "ಇದು ನಿಮ್ಮ ಶಕ್ತಿ ಮತ್ತು ದೇಹ ಹೇಗೆ ನಿಲ್ಲುತ್ತದೆ ಎಂಬುದರ ಬಗ್ಗೆ.",
                "ಇದು ಹಣ ಬರುವುದು ಮತ್ತು ಅದು ಉಳಿಯುವುದರ ಬಗ್ಗೆ.",
                "ಇದು ನೀವು ಮಾಡುವ ಕೆಲಸ ಮತ್ತು ನಿಮ್ಮ ಹೆಸರಿನ ಬಗ್ಗೆ.",
                "ಇದು ವ್ಯಾಪಾರ, ಒಪ್ಪಂದಗಳು ಮತ್ತು ಸ್ವಂತ ಗಳಿಕೆಯ ಬಗ್ಗೆ.",
                "ಇದು ಸಂಗಾತಿ ಮತ್ತು ವಿವಾಹ ಜೀವನದ ಬಗ್ಗೆ.",
                "ಇದು ಮಕ್ಕಳು ಮತ್ತು ಅವರೊಂದಿಗಿನ ನಿಮ್ಮ ಬಾಂಧವ್ಯದ ಬಗ್ಗೆ.",
                "ಇದು ಮನೆ, ಭೂಮಿ ಮತ್ತು ನಿಮ್ಮ ಸ್ವತ್ತಿನ ಬಗ್ಗೆ.",
                "ಇದು ಓದು ಮತ್ತು ಕಲಿಕೆಯ ಬಗ್ಗೆ.",
                "ಇದು ತಾಯಿ ತಂದೆಯೊಂದಿಗಿನ ನಿಮ್ಮ ಸಂಬಂಧದ ಬಗ್ಗೆ.",
                "ಇದು ಸುಖ, ಪ್ರಯಾಣ ಮತ್ತು ವಾಹನಗಳ ಬಗ್ಗೆ.",
                "ಇದು ನಂಬಿಕೆ ಮತ್ತು ಒಳಗಿನ ಹುಡುಕಾಟದ ಬಗ್ಗೆ.",
                "ಇದು ಕಷ್ಟ ಬಂದಾಗ ನೀವು ಎಷ್ಟು ಬೇಗ ಚೇತರಿಸಿಕೊಳ್ಳುತ್ತೀರಿ ಎಂಬುದರ ಬಗ್ಗೆ.",
                "ಇದು ಜೀವನದಲ್ಲಿ ಬರುವ ಅಡೆತಡೆಗಳ ಬಗ್ಗೆ.",
                "ಇದು ಕುಟುಂಬದ ಬೇರುಗಳು ಮತ್ತು ಹಿಂದಿನ ತಲೆಮಾರುಗಳಿಂದ ಬಂದದ್ದರ ಬಗ್ಗೆ."
            },
            _ => new[]
            {
                "This is about who you are and how people experience you.",
                "This is about your energy and how the body holds up.",
                "This is about money coming in and staying with you.",
                "This is about the work you do and the name you build.",
                "This is about trade, deals, and earning on your own.",
                "This is about partnership and married life.",
                "This is about children and your bond with them.",
                "This is about home, land, and what you own.",
                "This is about learning and how studies go.",
                "This is about your relationship with your parents.",
                "This is about comforts, travel, and vehicles.",
                "This is about faith and the inner search.",
                "This is about how well you recover when life gets hard.",
                "This is about the setbacks you meet.",
                "This is about family roots and what comes from earlier generations."
            }
        };

        return table[index];
    }

    private static string Outcome(string verdict, string locale) => (verdict, locale) switch
    {
        ("supported", "hi") => "सरल शब्दों में, इस हिस्से में मदद रुकावट से ज़्यादा है।",
        ("supported", "kn") => "ಸರಳವಾಗಿ ಹೇಳಿದರೆ, ಈ ಭಾಗದಲ್ಲಿ ನೆರವು ಅಡೆತಡೆಗಿಂತ ಹೆಚ್ಚು.",
        ("supported", _) => "In plain words, this part of life has more help than strain.",
        ("needsCare", "hi") => "सरल शब्दों में, इस हिस्से में ज़्यादा सावधानी चाहिए, क्योंकि रुकावट मदद से भारी है।",
        ("needsCare", "kn") => "ಸರಳವಾಗಿ ಹೇಳಿದರೆ, ಈ ಭಾಗಕ್ಕೆ ಹೆಚ್ಚು ಎಚ್ಚರಿಕೆ ಬೇಕು, ಏಕೆಂದರೆ ಅಡೆತಡೆ ನೆರವಿಗಿಂತ ಭಾರವಾಗಿದೆ.",
        ("needsCare", _) => "In plain words, this part of life needs extra care, because the strain outweighs the help.",
        (_, "hi") => "सरल शब्दों में, यह हिस्सा मिला-जुला है: कुछ बातें मदद करती हैं और कुछ रोकती हैं।",
        (_, "kn") => "ಸರಳವಾಗಿ ಹೇಳಿದರೆ, ಈ ಭಾಗ ಮಿಶ್ರವಾಗಿದೆ: ಕೆಲವು ವಿಷಯಗಳು ನೆರವಾಗುತ್ತವೆ, ಕೆಲವು ತಡೆಯುತ್ತವೆ.",
        _ => "In plain words, this part of life is uneven: some things help and some things hold it back."
    };

    private static string Charts(string specialist, int agree, int differ, string locale)
    {
        if (specialist == "D1")
        {
            return locale switch
            {
                "hi" => "यह पढ़ना जन्म कुंडली पर है, जो जीवन की बड़ी तस्वीर है।",
                "kn" => "ಈ ಓದು ಜನ್ಮ ಕುಂಡಲಿಯ ಮೇಲಿದೆ, ಅದು ಜೀವನದ ದೊಡ್ಡ ಚಿತ್ರ.",
                _ => "This reading uses the birth chart, which is the broad picture of life."
            };
        }

        var chart = ChartInWords(specialist, locale);
        if (agree >= differ)
        {
            return locale switch
            {
                "hi" => $"जन्म कुंडली और {chart} ज़्यादातर एक ही बात कहते हैं, इसलिए तस्वीर ज़्यादा स्थिर है।",
                "kn" => $"ಜನ್ಮ ಕುಂಡಲಿ ಮತ್ತು {chart} ಹೆಚ್ಚಾಗಿ ಒಂದೇ ಮಾತು ಹೇಳುತ್ತವೆ, ಆದ್ದರಿಂದ ಚಿತ್ರ ಹೆಚ್ಚು ಸ್ಥಿರವಾಗಿದೆ.",
                _ => $"The birth chart and the {chart} mostly say the same thing, so the picture is steadier."
            };
        }

        return locale switch
        {
            "hi" => $"जन्म कुंडली और {chart} पूरी तरह एक मत नहीं हैं, इसलिए आसानी और अड़चन दोनों दिख सकती हैं।",
            "kn" => $"ಜನ್ಮ ಕುಂಡಲಿ ಮತ್ತು {chart} ಸಂಪೂರ್ಣವಾಗಿ ಒಂದೇ ಅಭಿಪ್ರಾಯದಲ್ಲಿಲ್ಲ, ಆದ್ದರಿಂದ ಸುಲಭ ಮತ್ತು ತೊಂದರೆ ಎರಡೂ ಕಾಣಬಹುದು.",
            _ => $"The birth chart and the {chart} do not fully agree, so both ease and friction can show up."
        };
    }

    private static string ChartInWords(string code, string locale)
    {
        var index = code switch
        {
            "D2" => 0,
            "D3" => 1,
            "D4" => 2,
            "D7" => 3,
            "D9" => 4,
            "D10" => 5,
            "D12" => 6,
            "D16" => 7,
            "D20" => 8,
            "D24" => 9,
            "D27" => 10,
            "D30" => 11,
            "D40" => 12,
            "D45" => 13,
            "D60" => 14,
            _ => 15
        };
        var table = locale switch
        {
            "hi" => new[]
            {
                "धन चार्ट (D2)", "साहस चार्ट (D3)", "घर चार्ट (D4)", "संतान चार्ट (D7)",
                "विवाह चार्ट (D9)", "करियर चार्ट (D10)", "माता-पिता चार्ट (D12)", "सुख चार्ट (D16)",
                "साधना चार्ट (D20)", "शिक्षा चार्ट (D24)", "शक्ति चार्ट (D27)", "कठिनाई चार्ट (D30)",
                "मातृ कुल चार्ट (D40)", "पितृ कुल चार्ट (D45)", "सूक्ष्म चार्ट (D60)", $"{code} चार्ट"
            },
            "kn" => new[]
            {
                "ಧನ ಚಾರ್ಟ್ (D2)", "ಧೈರ್ಯ ಚಾರ್ಟ್ (D3)", "ಮನೆ ಚಾರ್ಟ್ (D4)", "ಮಕ್ಕಳ ಚಾರ್ಟ್ (D7)",
                "ವಿವಾಹ ಚಾರ್ಟ್ (D9)", "ವೃತ್ತಿ ಚಾರ್ಟ್ (D10)", "ಪೋಷಕರ ಚಾರ್ಟ್ (D12)", "ಸುಖ ಚಾರ್ಟ್ (D16)",
                "ಸಾಧನೆ ಚಾರ್ಟ್ (D20)", "ಶಿಕ್ಷಣ ಚಾರ್ಟ್ (D24)", "ಶಕ್ತಿ ಚಾರ್ಟ್ (D27)", "ಕಷ್ಟ ಚಾರ್ಟ್ (D30)",
                "ತಾಯಿ ಕುಲ ಚಾರ್ಟ್ (D40)", "ತಂದೆ ಕುಲ ಚಾರ್ಟ್ (D45)", "ಸೂಕ್ಷ್ಮ ಚಾರ್ಟ್ (D60)", $"{code} ಚಾರ್ಟ್"
            },
            _ => new[]
            {
                "wealth chart (D2)", "courage chart (D3)", "home chart (D4)", "children chart (D7)",
                "marriage chart (D9)", "career chart (D10)", "parents chart (D12)", "comforts chart (D16)",
                "spiritual chart (D20)", "education chart (D24)", "strength chart (D27)", "difficulty chart (D30)",
                "maternal-line chart (D40)", "paternal-line chart (D45)", "fine-detail chart (D60)", $"{code} chart"
            }
        };
        return table[index];
    }

    private static string Period(bool timingAvailable, bool connected, string locale)
    {
        if (!timingAvailable)
        {
            return locale switch
            {
                "hi" => "अभी चल रही ग्रह अवधि इस कुंडली से नहीं पढ़ी जा सकी।",
                "kn" => "ಈಗ ನಡೆಯುತ್ತಿರುವ ಗ್ರಹ ಅವಧಿಯನ್ನು ಈ ಕುಂಡಲಿಯಿಂದ ಓದಲಾಗಲಿಲ್ಲ.",
                _ => "The planetary period running now could not be read from this chart."
            };
        }

        if (connected)
        {
            return locale switch
            {
                "hi" => "अभी चल रही ग्रह अवधि इसी क्षेत्र से जुड़ी है, इसलिए यह पढ़ना इस समय ज़्यादा मायने रखता है।",
                "kn" => "ಈಗ ನಡೆಯುತ್ತಿರುವ ಗ್ರಹ ಅವಧಿ ಈ ಕ್ಷೇತ್ರಕ್ಕೆ ಸೇರಿದೆ, ಆದ್ದರಿಂದ ಈ ಓದು ಈಗ ಹೆಚ್ಚು ಮುಖ್ಯ.",
                _ => "The planetary period running now is linked to this area, so this reading matters more at the present time."
            };
        }

        return locale switch
        {
            "hi" => "अभी चल रही ग्रह अवधि इस क्षेत्र की मुख्य चालक नहीं है, इसलिए यह प्रभाव अभी पृष्ठभूमि में है।",
            "kn" => "ಈಗ ನಡೆಯುತ್ತಿರುವ ಗ್ರಹ ಅವಧಿ ಈ ಕ್ಷೇತ್ರದ ಮುಖ್ಯ ಚಾಲಕವಲ್ಲ, ಆದ್ದರಿಂದ ಈ ಪ್ರಭಾವ ಈಗ ಹಿನ್ನೆಲೆಯಲ್ಲಿದೆ.",
            _ => "The planetary period running now is not the main driver of this area, so this influence sits in the background for now."
        };
    }
}
