using KundaliEngine.API.Services.Muhurtha.Models;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaGenericTimeWindowService
{
    IReadOnlyList<MuhurthaTimeWindow> BuildWindows(MuhurthaDayContext dayContext);
}
