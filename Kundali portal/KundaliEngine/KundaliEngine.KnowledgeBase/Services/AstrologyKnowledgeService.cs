using System.Text.Json;
using KundaliEngine.KnowledgeBase.Models;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class AstrologyKnowledgeService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IReadOnlyList<PlanetKnowledge> _planets;
        private readonly IReadOnlyList<HouseKnowledge> _houses;
        private readonly IReadOnlyList<StrengthKnowledge> _strengths;

        public AstrologyKnowledgeService()
            : this(AppContext.BaseDirectory)
        {
        }

        public AstrologyKnowledgeService(string basePath)
        {
            _planets = Load<PlanetKnowledgeRoot>(basePath, "Data", "Planets", "planets.json").Planets;
            _houses = Load<HouseKnowledgeRoot>(basePath, "Data", "Houses", "houses.json").Houses;
            _strengths = Load<StrengthKnowledgeRoot>(basePath, "Data", "Strengths", "strengths.json").Strengths;
        }

        public IReadOnlyList<PlanetKnowledge> GetPlanets()
        {
            return _planets;
        }

        public IReadOnlyList<HouseKnowledge> GetHouses()
        {
            return _houses;
        }

        public IReadOnlyList<StrengthKnowledge> GetStrengths()
        {
            return _strengths;
        }

        public PlanetKnowledge? GetPlanet(string name)
        {
            return _planets.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || p.SanskritName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public HouseKnowledge? GetHouse(int number)
        {
            return _houses.FirstOrDefault(h => h.Number == number);
        }

        public StrengthKnowledge? GetStrength(string name)
        {
            return _strengths.FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static T Load<T>(string basePath, params string[] pathParts)
        {
            var path = Path.Combine(new[] { basePath }.Concat(pathParts).ToArray());

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Astrology knowledge file was not found: {path}", path);
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException($"Astrology knowledge file is empty or invalid: {path}");
        }
    }
}
