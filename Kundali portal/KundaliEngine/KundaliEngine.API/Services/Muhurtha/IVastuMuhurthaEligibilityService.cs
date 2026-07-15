using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IVastuMuhurthaEligibilityService
{
    bool ShouldApply(MuhurthaEventType eventType);

    VastuMuhurthaEligibilityResult? Evaluate(MuhurthaRequest request, MuhurthaCandidateSlot candidate);
}

