using KundaliEngine.API.DTOs.Requests.Numerology;
using KundaliEngine.API.DTOs.Responses.Numerology;
using KundaliEngine.KnowledgeBase.Services.Numerology;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers.Numerology
{
    [ApiController]
    [Route("api/[controller]")]
    public class NumerologyController : ControllerBase
    {
        private readonly NumerologyCalculator _calculator;
        private readonly NumerologyMetadataService _numerologyMetadataService;

        public NumerologyController(NumerologyCalculator calculator, NumerologyMetadataService numerologyMetadataService)
        {
            _calculator = calculator;
            _numerologyMetadataService = numerologyMetadataService;
        }

        [HttpPost("calculate")]
        public ActionResult<NumerologyResponse> Calculate([FromBody] NumerologyRequest request)
        {
            if (request.DateOfBirth == default)
            {
                return BadRequest("Date of birth is required.");
            }

            if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return BadRequest("Date of birth cannot be in the future.");
            }

            NumerologyNameValue? nameValue = null;
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                if (!_calculator.TryCalculateName(request.Name, out nameValue) || nameValue is null)
                {
                    return BadRequest("Enter a name using English letters.");
                }
            }

            NumerologyMobileValue? mobileValue = null;
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                if (!_calculator.TryCalculateMobile(request.MobileNumber, out mobileValue) || mobileValue is null)
                {
                    return BadRequest("Enter a mobile number that includes digits.");
                }
            }

            NumerologyNameValue? businessValue = null;
            if (!string.IsNullOrWhiteSpace(request.BusinessName))
            {
                if (!_calculator.TryCalculateName(request.BusinessName, out businessValue) || businessValue is null)
                {
                    return BadRequest("Enter a business name using English letters.");
                }
            }

            var response = _calculator.Calculate(request.DateOfBirth);

            var mulankMetadata = _numerologyMetadataService.GetNumber(response.Mulank);
            var bhagyankMetadata = _numerologyMetadataService.GetNumber(response.Bhagyank);

            if (mulankMetadata != null)
            {
                response.MulankDetails = new NumerologyNumberDetailsResponse
                {
                    Summary = mulankMetadata.Summary,
                    Traits = mulankMetadata.Traits,
                    Strengths = mulankMetadata.Strengths,
                    Challenges = mulankMetadata.Challenges,
                    FavourableNumbers = mulankMetadata.FavourableNumbers,
                    FavourableDays = mulankMetadata.FavourableDays,
                    FavourableColours = mulankMetadata.FavourableColours
                };
            }

            if (bhagyankMetadata != null)
            {
                response.BhagyankDetails = new NumerologyNumberDetailsResponse
                {
                    Summary = bhagyankMetadata.Summary,
                    Traits = bhagyankMetadata.Traits,
                    Strengths = bhagyankMetadata.Strengths,
                    Challenges = bhagyankMetadata.Challenges,
                    FavourableNumbers = bhagyankMetadata.FavourableNumbers,
                    FavourableDays = bhagyankMetadata.FavourableDays,
                    FavourableColours = bhagyankMetadata.FavourableColours
                };
            }

            var combination = _numerologyMetadataService.GetCombination(response.Mulank, response.Bhagyank);

            if (combination != null)
            {
                response.CombinationType = combination.Type;
                response.CombinationSummary = combination.Summary;
                response.CombinationStrengths = combination.Strengths;
                response.CombinationChallenges = combination.Challenges;
            }

            var supportive = NumerologyCalculator.SupportiveNumbers(
                response.Mulank,
                response.Bhagyank,
                mulankMetadata?.FavourableNumbers,
                bhagyankMetadata?.FavourableNumbers);

            if (nameValue is not null)
            {
                response.Name = nameValue.Name;
                response.NameCompound = nameValue.Compound;
                response.NameNumber = nameValue.Number;
                response.NamePlanet = nameValue.Planet;
                response.NameCalculation = nameValue.Calculation;
                response.NameMulankMatch = NameMatch(nameValue.Number, response.Mulank, mulankMetadata?.FavourableNumbers);
                response.NameBhagyankMatch = NameMatch(nameValue.Number, response.Bhagyank, bhagyankMetadata?.FavourableNumbers);
            }

            if (mobileValue is not null)
            {
                var mulankMatch = NameMatch(mobileValue.Number, response.Mulank, mulankMetadata?.FavourableNumbers);
                var bhagyankMatch = NameMatch(mobileValue.Number, response.Bhagyank, bhagyankMetadata?.FavourableNumbers);
                response.Mobile = new NumerologyValueCheckResponse
                {
                    Label = mobileValue.Mobile,
                    Compound = mobileValue.Compound,
                    Number = mobileValue.Number,
                    Planet = mobileValue.Planet,
                    Calculation = mobileValue.Calculation,
                    MulankMatch = mulankMatch,
                    BhagyankMatch = bhagyankMatch,
                    Suggestions = NeedsSuggestions(mulankMatch, bhagyankMatch)
                        ? _calculator.SuggestMobileCorrections(mobileValue, supportive).ToList()
                        : []
                };
            }

            if (businessValue is not null)
            {
                var mulankMatch = NameMatch(businessValue.Number, response.Mulank, mulankMetadata?.FavourableNumbers);
                var bhagyankMatch = NameMatch(businessValue.Number, response.Bhagyank, bhagyankMetadata?.FavourableNumbers);
                response.Business = new NumerologyValueCheckResponse
                {
                    Label = businessValue.Name,
                    Compound = businessValue.Compound,
                    Number = businessValue.Number,
                    Planet = businessValue.Planet,
                    Calculation = businessValue.Calculation,
                    MulankMatch = mulankMatch,
                    BhagyankMatch = bhagyankMatch,
                    Suggestions = NeedsSuggestions(mulankMatch, bhagyankMatch)
                        ? _calculator.SuggestNameCorrections(businessValue, supportive).ToList()
                        : []
                };
            }

            return Ok(response);
        }

        private static string NameMatch(int valueNumber, int target, IReadOnlyList<int>? favourable)
        {
            return valueNumber == target || favourable?.Contains(valueNumber) == true
                ? "Supports"
                : "Differs";
        }

        private static bool NeedsSuggestions(string mulankMatch, string bhagyankMatch)
            => mulankMatch == "Differs" || bhagyankMatch == "Differs";
    }
}
