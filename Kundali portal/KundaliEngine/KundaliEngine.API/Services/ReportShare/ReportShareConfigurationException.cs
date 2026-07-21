namespace KundaliEngine.API.Services.ReportShare;

public sealed class ReportShareConfigurationException : InvalidOperationException
{
    public ReportShareConfigurationException(string message)
        : base(message)
    {
    }
}