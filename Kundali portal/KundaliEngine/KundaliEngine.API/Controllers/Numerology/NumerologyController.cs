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

        public NumerologyController(NumerologyCalculator calculator,  NumerologyMetadataService numerologyMetadataService )
        {
            _calculator = calculator;
            _numerologyMetadataService= numerologyMetadataService;

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

            var combination = _numerologyMetadataService.GetCombination(response.Mulank,response.Bhagyank);

            if (combination != null)
            {
                response.CombinationType = combination.Type;
                response.CombinationSummary = combination.Summary;
                response.CombinationStrengths = combination.Strengths;
                response.CombinationChallenges = combination.Challenges;
            }

            return Ok(response);
        }
    }
}
