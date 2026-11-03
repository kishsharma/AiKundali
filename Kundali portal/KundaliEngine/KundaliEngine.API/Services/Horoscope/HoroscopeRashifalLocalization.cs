namespace KundaliEngine.API.Services.Horoscope;

internal static class HoroscopeRashifalLocalization
{
    private static readonly string[] Signs = [
        "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    ];

    private static readonly string[] HindiSigns = [
        "मेष", "वृषभ", "मिथुन", "कर्क", "सिंह", "कन्या",
        "तुला", "वृश्चिक", "धनु", "मकर", "कुंभ", "मीन"
    ];

    private static readonly string[] KannadaSigns = [
        "ಮೇಷ", "ವೃಷಭ", "ಮಿಥುನ", "ಕರ್ಕಾಟಕ", "ಸಿಂಹ", "ಕನ್ಯಾ",
        "ತುಲಾ", "ವೃಶ್ಚಿಕ", "ಧನು", "ಮಕರ", "ಕುಂಭ", "ಮೀನ"
    ];

    private static readonly Dictionary<string, (string Hindi, string Kannada)> TransitLines = new(StringComparer.Ordinal)
    {
        ["Jupiter in the 2nd from Moon favours speech, savings, and family support."] = ("चंद्र राशि से दूसरे भाव में बृहस्पति वाणी, बचत और पारिवारिक सहयोग को बढ़ावा देता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 2ನೇ ಮನೆಯಲ್ಲಿ ಗುರು ಮಾತು, ಉಳಿತಾಯ ಮತ್ತು ಕುಟುಂಬದ ಬೆಂಬಲಕ್ಕೆ ಅನುಕೂಲವಾಗುತ್ತಾನೆ."),
        ["Jupiter in the 5th from Moon lifts intellect, creativity, and goodwill."] = ("चंद्र राशि से पाँचवें भाव में बृहस्पति बुद्धि, रचनात्मकता और सद्भाव को बढ़ाता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 5ನೇ ಮನೆಯಲ್ಲಿ ಗುರು ಬುದ್ಧಿವಂತಿಕೆ, ಸೃಜನಶೀಲತೆ ಮತ್ತು ಸೌಹಾರ್ದವನ್ನು ಹೆಚ್ಚಿಸುತ್ತಾನೆ."),
        ["Jupiter in the 7th from Moon blesses partnerships and agreements."] = ("चंद्र राशि से सातवें भाव में बृहस्पति साझेदारी और समझौतों के लिए सहायक है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 7ನೇ ಮನೆಯಲ್ಲಿ ಗುರು ಪಾಲುದಾರಿಕೆ ಮತ್ತು ಒಪ್ಪಂದಗಳಿಗೆ ಬೆಂಬಲ ನೀಡುತ್ತಾನೆ."),
        ["Jupiter in the 9th from Moon opens luck, mentors, and longer journeys."] = ("चंद्र राशि से नौवें भाव में बृहस्पति भाग्य, मार्गदर्शन और लंबी यात्राओं के अवसर खोलता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 9ನೇ ಮನೆಯಲ್ಲಿ ಗುರು ಅದೃಷ್ಟ, ಮಾರ್ಗದರ್ಶನ ಮತ್ತು ದೂರ ಪ್ರಯಾಣದ ಅವಕಾಶಗಳನ್ನು ಒದಗಿಸುತ್ತಾನೆ."),
        ["Jupiter in the 11th from Moon strengthens gains and networks."] = ("चंद्र राशि से ग्यारहवें भाव में बृहस्पति लाभ और संपर्कों को मजबूत करता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 11ನೇ ಮನೆಯಲ್ಲಿ ಗುರು ಲಾಭ ಮತ್ತು ಸಂಪರ್ಕ ಜಾಲವನ್ನು ಬಲಪಡಿಸುತ್ತಾನೆ."),
        ["Saturn on the Moon sign (Sade-Sati zone) asks discipline and emotional steadiness."] = ("चंद्र राशि पर शनि (साढ़ेसाती क्षेत्र) अनुशासन और भावनात्मक स्थिरता की मांग करता है।", "ಚಂದ್ರ ರಾಶಿಯಲ್ಲಿರುವ ಶನಿ (ಸಾಡೇಸಾತಿ ವಲಯ) ಶಿಸ್ತು ಮತ್ತು ಭಾವನಾತ್ಮಕ ಸ್ಥಿರತೆಯನ್ನು ಕೋರುತ್ತಾನೆ."),
        ["Saturn in the 8th from Moon (Ashtama) brings delays and deeper review."] = ("चंद्र राशि से आठवें भाव में शनि (अष्टम शनि) देरी और गहन पुनर्विचार ला सकता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 8ನೇ ಮನೆಯಲ್ಲಿ ಶನಿ (ಅಷ್ಟಮ ಶನಿ) ವಿಳಂಬ ಮತ್ತು ಆಳವಾದ ಪರಿಶೀಲನೆಯನ್ನು ತರಬಹುದು."),
        ["Saturn in the 12th from Moon emphasises solitude, expenses, and rest."] = ("चंद्र राशि से बारहवें भाव में शनि एकांत, खर्च और विश्राम पर ध्यान देने को कहता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 12ನೇ ಮನೆಯಲ್ಲಿ ಶನಿ ಏಕಾಂತ, ಖರ್ಚು ಮತ್ತು ವಿಶ್ರಾಂತಿಗೆ ಗಮನ ನೀಡುವಂತೆ ಸೂಚಿಸುತ್ತಾನೆ."),
        ["Saturn in the 4th from Moon pressures home peace and inner ease."] = ("चंद्र राशि से चौथे भाव में शनि घरेलू शांति और मन की सहजता पर दबाव डाल सकता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 4ನೇ ಮನೆಯಲ್ಲಿ ಶನಿ ಮನೆಯ ನೆಮ್ಮದಿ ಮತ್ತು ಮನಸ್ಸಿನ ಆರಾಮಕ್ಕೆ ಒತ್ತಡ ತರಬಹುದು."),
        ["Saturn in the 7th from Moon tests partnerships with duty and distance."] = ("चंद्र राशि से सातवें भाव में शनि साझेदारी में जिम्मेदारी और दूरी की परीक्षा ले सकता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 7ನೇ ಮನೆಯಲ್ಲಿ ಶನಿ ಪಾಲುದಾರಿಕೆಯಲ್ಲಿ ಜವಾಬ್ದಾರಿ ಮತ್ತು ಅಂತರವನ್ನು ಪರೀಕ್ಷಿಸಬಹುದು."),
        ["Saturn in the 10th from Moon slows career progress but builds endurance."] = ("चंद्र राशि से दसवें भाव में शनि करियर की गति धीमी कर सकता है, पर धैर्य बढ़ाता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 10ನೇ ಮನೆಯಲ್ಲಿ ಶನಿ ವೃತ್ತಿ ಪ್ರಗತಿಯನ್ನು ನಿಧಾನಗೊಳಿಸಬಹುದು, ಆದರೆ ತಾಳ್ಮೆಯನ್ನು ಬೆಳೆಸುತ್ತಾನೆ."),
        ["Mars on the Moon sign raises heat, haste, and short tempers."] = ("चंद्र राशि पर मंगल आवेश, जल्दबाजी और चिड़चिड़ापन बढ़ा सकता है।", "ಚಂದ್ರ ರಾಶಿಯಲ್ಲಿರುವ ಮಂಗಳ ಆವೇಶ, ಆತುರ ಮತ್ತು ಸಿಡುಕನ್ನು ಹೆಚ್ಚಿಸಬಹುದು."),
        ["Mars in the 8th from Moon warns against risk and accidents."] = ("चंद्र राशि से आठवें भाव में मंगल जोखिम और दुर्घटनाओं से सावधान रहने का संकेत देता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 8ನೇ ಮನೆಯಲ್ಲಿ ಮಂಗಳ ಅಪಾಯ ಮತ್ತು ಅಪಘಾತಗಳ ಬಗ್ಗೆ ಎಚ್ಚರಿಕೆ ನೀಡುತ್ತಾನೆ."),
        ["Rahu on the Moon sign stirs restlessness and sudden shifts."] = ("चंद्र राशि पर राहु बेचैनी और अचानक बदलाव ला सकता है।", "ಚಂದ್ರ ರಾಶಿಯಲ್ಲಿರುವ ರಾಹು ಚಂಚಲತೆ ಮತ್ತು ಅನಿರೀಕ್ಷಿತ ಬದಲಾವಣೆಗಳನ್ನು ತರಬಹುದು."),
        ["Ketu on the Moon sign thins emotional certainty; keep routines."] = ("चंद्र राशि पर केतु भावनात्मक अनिश्चितता ला सकता है; नियमित दिनचर्या बनाए रखें।", "ಚಂದ್ರ ರಾಶಿಯಲ್ಲಿರುವ ಕೇತು ಭಾವನಾತ್ಮಕ ಅನಿಶ್ಚಿತತೆಯನ್ನು ತರಬಹುದು; ದಿನಚರಿಯನ್ನು ಪಾಲಿಸಿ."),
        ["Venus in the 7th from Moon softens relations and aesthetic luck."] = ("चंद्र राशि से सातवें भाव में शुक्र संबंधों में मधुरता और सौंदर्यबोध को बढ़ाता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 7ನೇ ಮನೆಯಲ್ಲಿ ಶುಕ್ರ ಸಂಬಂಧಗಳಲ್ಲಿ ಸೌಹಾರ್ದ ಮತ್ತು ಸೌಂದರ್ಯಭಾವವನ್ನು ಹೆಚ್ಚಿಸುತ್ತಾನೆ."),
        ["Venus in the 4th from Moon comforts home and heart."] = ("चंद्र राशि से चौथे भाव में शुक्र घर और मन को सुख देता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 4ನೇ ಮನೆಯಲ್ಲಿ ಶುಕ್ರ ಮನೆ ಮತ್ತು ಮನಸ್ಸಿಗೆ ಆರಾಮ ನೀಡುತ್ತಾನೆ."),
        ["Mercury in the 3rd from Moon aids writing, deals, and short travel."] = ("चंद्र राशि से तीसरे भाव में बुध लेखन, बातचीत और छोटी यात्राओं में सहायता करता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 3ನೇ ಮನೆಯಲ್ಲಿ ಬುಧ ಬರವಣಿಗೆ, ಮಾತುಕತೆ ಮತ್ತು ಕಿರು ಪ್ರಯಾಣಗಳಿಗೆ ಸಹಾಯ ಮಾಡುತ್ತಾನೆ."),
        ["Sun in the 10th from Moon highlights authority and recognition."] = ("चंद्र राशि से दसवें भाव में सूर्य अधिकार और पहचान को उजागर करता है।", "ಚಂದ್ರ ರಾಶಿಯಿಂದ 10ನೇ ಮನೆಯಲ್ಲಿ ಸೂರ್ಯ ಅಧಿಕಾರ ಮತ್ತು ಗುರುತನ್ನು ತರುತ್ತಾನೆ."),
        ["Slow planets set the broader tone from Chandra lagna."] = ("चंद्र लग्न से धीमे ग्रह व्यापक प्रवृत्ति तय करते हैं।", "ಚಂದ್ರ ಲಗ್ನದಿಂದ ನಿಧಾನಗತಿಯ ಗ್ರಹಗಳು ಒಟ್ಟಾರೆ ಪ್ರವೃತ್ತಿಯನ್ನು ರೂಪಿಸುತ್ತವೆ.")
    };

    private static readonly Dictionary<string, (string Hindi, string Kannada)> AreaLines = new(StringComparer.Ordinal)
    {
        ["career|Supportive"] = ("काम में पहचान बढ़ेगी; नियोजित कार्य आगे बढ़ाएँ।", "ಕೆಲಸದಲ್ಲಿ ಗುರುತಿಸುವಿಕೆ ಹೆಚ್ಚುತ್ತದೆ; ಯೋಜಿತ ಕಾರ್ಯಗಳನ್ನು ಮುಂದುವರಿಸಿ."),
        ["career|Challenging"] = ("करियर में धैर्य रखें; नौकरी में अचानक बदलाव से बचें।", "ವೃತ್ತಿಯಲ್ಲಿ ತಾಳ್ಮೆ ಇರಲಿ; ಉದ್ಯೋಗದಲ್ಲಿ ಹಠಾತ್ ಬದಲಾವಣೆಗಳನ್ನು ತಪ್ಪಿಸಿ."),
        ["money|Supportive"] = ("आय और संसाधन अधिक स्थिर दिखते हैं।", "ಆದಾಯ ಮತ್ತು ಸಂಪನ್ಮೂಲಗಳು ಹೆಚ್ಚು ಸ್ಥಿರವಾಗುವ ಸೂಚನೆ ಇದೆ."),
        ["money|Challenging"] = ("खर्च सोच-समझकर करें; सट्टा आधारित निवेश टालें।", "ಖರ್ಚನ್ನು ಎಚ್ಚರಿಕೆಯಿಂದ ಮಾಡಿ; ಊಹಾಪೋಹದ ಹೂಡಿಕೆಗಳನ್ನು ಮುಂದೂಡಿ."),
        ["health|Supportive"] = ("नियमित दिनचर्या बनाए रखने पर ऊर्जा बनी रहेगी।", "ನಿಯಮಿತ ದಿನಚರಿಯನ್ನು ಪಾಲಿಸಿದರೆ ಚೈತನ್ಯ ಉಳಿಯುತ್ತದೆ."),
        ["health|Challenging"] = ("आराम का ध्यान रखें और शरीर पर अधिक जोर न डालें।", "ವಿಶ್ರಾಂತಿಗೆ ಆದ್ಯತೆ ನೀಡಿ ಮತ್ತು ದೇಹಕ್ಕೆ ಅತಿಯಾದ ಒತ್ತಡ ಕೊಡಬೇಡಿ."),
        ["relations|Supportive"] = ("संबंधों में अपनापन और स्पष्टता से लाभ होगा।", "ಸಂಬಂಧಗಳಲ್ಲಿ ಆತ್ಮೀಯತೆ ಮತ್ತು ಸ್ಪಷ್ಟತೆ ಉತ್ತಮ ಫಲ ನೀಡುತ್ತದೆ."),
        ["relations|Challenging"] = ("बातचीत संयम से करें; अहं के टकराव से बचें।", "ಮಾತುಕತೆಯಲ್ಲಿ ಸಂಯಮ ಇರಲಿ; ಅಹಂ ಘರ್ಷಣೆಯನ್ನು ತಪ್ಪಿಸಿ."),
        ["mind|Supportive"] = ("ध्यान और अध्ययन करना सहज होगा।", "ಏಕಾಗ್ರತೆ ಮತ್ತು ಅಧ್ಯಯನ ಸುಲಭವಾಗುತ್ತದೆ."),
        ["mind|Challenging"] = ("मन को शांत रखें; बड़े फैसले कुछ समय टालें।", "ಮನಸ್ಸನ್ನು ಶಾಂತವಾಗಿರಿಸಿ; ದೊಡ್ಡ ನಿರ್ಧಾರಗಳನ್ನು ಸ್ವಲ್ಪ ಮುಂದೂಡಿ."),
        ["home|Supportive"] = ("घर और सुख-सुविधा से जुड़े विषय सहज होंगे।", "ಮನೆ ಮತ್ತು ಸೌಕರ್ಯಗಳಿಗೆ ಸಂಬಂಧಿಸಿದ ವಿಷಯಗಳು ಸುಗಮವಾಗುತ್ತವೆ."),
        ["home|Challenging"] = ("घरेलू तनाव को धैर्य और कोमलता से संभालें।", "ಮನೆಯ ಒತ್ತಡವನ್ನು ತಾಳ್ಮೆ ಮತ್ತು ಸೌಮ್ಯತೆಯಿಂದ ನಿರ್ವಹಿಸಿ."),
        ["|Supportive"] = ("रचनात्मक प्रयासों के लिए परिस्थितियाँ अनुकूल हैं।", "ಸೃಜನಾತ್ಮಕ ಪ್ರಯತ್ನಗಳಿಗೆ ಪರಿಸ್ಥಿತಿಗಳು ಅನುಕೂಲಕರವಾಗಿವೆ."),
        ["|Challenging"] = ("धीरे आगे बढ़ें और अपनी ऊर्जा बचाकर रखें।", "ನಿಧಾನವಾಗಿ ಮುಂದುವರಿಯಿರಿ ಮತ್ತು ನಿಮ್ಮ ಶಕ್ತಿಯನ್ನು ಉಳಿಸಿಕೊಳ್ಳಿ."),
        ["|Mixed"] = ("परिणाम मिले-जुले रह सकते हैं; व्यावहारिक बने रहें।", "ಫಲಿತಾಂಶಗಳು ಮಿಶ್ರವಾಗಿರಬಹುದು; ವಾಸ್ತವಿಕವಾಗಿ ನಡೆದುಕೊಳ್ಳಿ.")
    };

    public static string LocalizeTransit(string english, string locale)
    {
        if (locale is not ("hi" or "kn"))
        {
            return english;
        }

        if (TransitLines.TryGetValue(english, out var known))
        {
            return locale == "hi" ? known.Hindi : known.Kannada;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            english,
            @"^(?<planet>\w+) in the (?<house>\d+)(?:st|nd|rd|th) from Moon (?<effect>supports steady progress|asks for patience and care|keeps results mixed)\.$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return english;
        }

        var planet = LocalizedPlanet(match.Groups["planet"].Value, locale);
        var house = int.Parse(match.Groups["house"].Value, System.Globalization.CultureInfo.InvariantCulture);
        if (locale == "hi")
        {
            var ordinal = house switch { 1 => "पहले", 2 => "दूसरे", 3 => "तीसरे", 4 => "चौथे", 5 => "पाँचवें", 6 => "छठे", 7 => "सातवें", 8 => "आठवें", 9 => "नौवें", 10 => "दसवें", 11 => "ग्यारहवें", 12 => "बारहवें", _ => $"{house}वें" };
            var effect = match.Groups["effect"].Value switch
            {
                "supports steady progress" => "स्थिर प्रगति में सहयोग देता है",
                "asks for patience and care" => "धैर्य और सावधानी रखने का संकेत देता है",
                _ => "मिले-जुले परिणाम देता है"
            };
            return $"चंद्र राशि से {ordinal} भाव में {planet} {effect}।";
        }

        var kannadaOrdinal = house switch { 1 => "1ನೇ", 2 => "2ನೇ", 3 => "3ನೇ", 4 => "4ನೇ", 5 => "5ನೇ", 6 => "6ನೇ", 7 => "7ನೇ", 8 => "8ನೇ", 9 => "9ನೇ", 10 => "10ನೇ", 11 => "11ನೇ", 12 => "12ನೇ", _ => $"{house}ನೇ" };
        var kannadaEffect = match.Groups["effect"].Value switch
        {
            "supports steady progress" => "ಸ್ಥಿರ ಪ್ರಗತಿಗೆ ಬೆಂಬಲ ನೀಡುತ್ತಾನೆ",
            "asks for patience and care" => "ತಾಳ್ಮೆ ಮತ್ತು ಎಚ್ಚರಿಕೆಯನ್ನು ಸೂಚಿಸುತ್ತಾನೆ",
            _ => "ಮಿಶ್ರ ಫಲಿತಾಂಶಗಳನ್ನು ನೀಡುತ್ತಾನೆ"
        };
        return $"ಚಂದ್ರ ರಾಶಿಯಿಂದ {kannadaOrdinal} ಮನೆಯಲ್ಲಿ {planet} {kannadaEffect}.";
    }

    public static string LocalizeArea(string key, string tone, string english, string locale) =>
        AreaLines.TryGetValue($"{key}|{tone}", out var translation)
            ? locale switch { "hi" => translation.Hindi, "kn" => translation.Kannada, _ => english }
            : GetText(AreaLines, $"|{tone}", locale, english);

    public static string BuildSummary(string englishRashi, string tone, IReadOnlyList<string> highlights, string locale)
    {
        if (locale is not ("hi" or "kn"))
        {
            var lead = tone switch
            {
                "Supportive" => $"For {englishRashi} Moon natives, the period leans supportive.",
                "Challenging" => $"For {englishRashi} Moon natives, the period asks caution.",
                _ => $"For {englishRashi} Moon natives, the period stays mixed."
            };
            var detail = highlights.Count > 0
                ? string.Join(" ", highlights.Take(2))
                : "Slow planets set the broader tone from Chandra lagna.";
            return $"{lead} {detail}";
        }

        var signIndex = Array.IndexOf(Signs, englishRashi);
        var sign = signIndex >= 0
            ? (locale == "hi" ? HindiSigns[signIndex] : KannadaSigns[signIndex])
            : englishRashi;
        var localizedLead = locale == "hi"
            ? tone switch
            {
                "Supportive" => $"{sign} राशि के जातकों के लिए यह अवधि अनुकूल है।",
                "Challenging" => $"{sign} राशि के जातक इस अवधि में सावधानी रखें।",
                _ => $"{sign} राशि के जातकों के लिए यह अवधि मिश्रित रहेगी।"
            }
            : tone switch
            {
                "Supportive" => $"{sign} ರಾಶಿಯವರಿಗೆ ಈ ಅವಧಿ ಅನುಕೂಲಕರವಾಗಿದೆ.",
                "Challenging" => $"{sign} ರಾಶಿಯವರು ಈ ಅವಧಿಯಲ್ಲಿ ಎಚ್ಚರಿಕೆಯಿಂದಿರಿ.",
                _ => $"{sign} ರಾಶಿಯವರಿಗೆ ಈ ಅವಧಿ ಮಿಶ್ರವಾಗಿರುತ್ತದೆ."
            };
        var localizedHighlights = highlights.Take(2).Select(line => LocalizeTransit(line, locale));
        var localizedDetail = highlights.Count > 0
            ? string.Join(" ", localizedHighlights)
            : locale == "hi"
                ? "चंद्र लग्न से धीमे ग्रह व्यापक प्रवृत्ति तय करते हैं।"
                : "ಚಂದ್ರ ಲಗ್ನದಿಂದ ನಿಧಾನಗತಿಯ ಗ್ರಹಗಳು ಒಟ್ಟಾರೆ ಪ್ರವೃತ್ತಿಯನ್ನು ರೂಪಿಸುತ್ತವೆ.";
        return $"{localizedLead} {localizedDetail}";
    }

    public static string Disclaimer(string locale) => locale switch
    {
        "hi" => "सामान्य चंद्र राशि पर आधारित राशिफल। समान चंद्र राशि वाले सभी लोगों के लिए समान। पारंपरिक ज्योतिषीय मार्गदर्शन; चिकित्सा, कानूनी या वित्तीय सलाह नहीं।",
        "kn" => "ಚಂದ್ರ ರಾಶಿ ಆಧಾರಿತ ಸಾಮಾನ್ಯ ರಾಶಿಫಲ. ಒಂದೇ ಚಂದ್ರ ರಾಶಿಯ ಎಲ್ಲರಿಗೂ ಒಂದೇ ಆಗಿರುತ್ತದೆ. ಇದು ಸಾಂಪ್ರದಾಯಿಕ ಜ್ಯೋತಿಷ್ಯ ಮಾರ್ಗದರ್ಶನ; ವೈದ್ಯಕೀಯ, ಕಾನೂನು ಅಥವಾ ಹಣಕಾಸು ಸಲಹೆಯಲ್ಲ.",
        _ => "General Moon-sign rashifal. Identical for every person sharing the same Chandra rashi. Traditional Jyotish judgment, not medical, legal, or financial advice."
    };

    private static string GetText(
        IReadOnlyDictionary<string, (string Hindi, string Kannada)> dictionary,
        string key,
        string locale,
        string? fallback = null) =>
        dictionary.TryGetValue(key, out var translation)
            ? locale switch { "hi" => translation.Hindi, "kn" => translation.Kannada, _ => fallback ?? key }
            : fallback ?? key;

    private static string LocalizedPlanet(string planet, string locale)
    {
        var hindi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sun"] = "सूर्य", ["Moon"] = "चंद्र", ["Mars"] = "मंगल", ["Mercury"] = "बुध",
            ["Jupiter"] = "बृहस्पति", ["Venus"] = "शुक्र", ["Saturn"] = "शनि", ["Rahu"] = "राहु", ["Ketu"] = "केतु"
        };
        var kannada = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sun"] = "ಸೂರ್ಯ", ["Moon"] = "ಚಂದ್ರ", ["Mars"] = "ಮಂಗಳ", ["Mercury"] = "ಬುಧ",
            ["Jupiter"] = "ಗುರು", ["Venus"] = "ಶುಕ್ರ", ["Saturn"] = "ಶನಿ", ["Rahu"] = "ರಾಹು", ["Ketu"] = "ಕೇತು"
        };
        var dictionary = locale == "hi" ? hindi : kannada;
        return dictionary.TryGetValue(planet, out var translated) ? translated : planet;
    }
}
