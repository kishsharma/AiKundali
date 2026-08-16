using KundaliEngine.API.Services.Muhurtha.Models;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaEclipseWindowService
{
    IReadOnlyList<MuhurthaTimeWindow> BuildWindows(MuhurthaDayContext dayContext);
}
