using System.Diagnostics;

namespace KundaliEngine.API.Services.ReportPdf.Html;

public interface IHtmlReportPdfRenderer
{
    bool CanRender { get; }
    byte[] RenderPdf(string html, string fileName);
}

public sealed class ChromeHtmlReportPdfRenderer : IHtmlReportPdfRenderer
{
    private static readonly string[] BrowserPaths =
    {
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    };

    private readonly ILogger<ChromeHtmlReportPdfRenderer> _logger;

    public ChromeHtmlReportPdfRenderer(ILogger<ChromeHtmlReportPdfRenderer> logger)
    {
        _logger = logger;
    }

    public bool CanRender => FindBrowserPath() is not null;

    public byte[] RenderPdf(string html, string fileName)
    {
        var browserPath = FindBrowserPath();
        if (browserPath is null)
        {
            throw new InvalidOperationException("No Chrome or Edge browser was found for HTML report rendering.");
        }

        var safeStem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(safeStem))
        {
            safeStem = "kundali-report";
        }

        var workDir = Path.Combine(Path.GetTempPath(), "horalife-report-pdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var htmlPath = Path.Combine(workDir, safeStem + ".html");
        var pdfPath = Path.Combine(workDir, safeStem + ".pdf");

        try
        {
            File.WriteAllText(htmlPath, html);
            var startInfo = new ProcessStartInfo
            {
                FileName = browserPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workDir
            };

            startInfo.ArgumentList.Add("--headless=new");
            startInfo.ArgumentList.Add("--disable-gpu");
            startInfo.ArgumentList.Add("--disable-extensions");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-default-browser-check");
            startInfo.ArgumentList.Add("--allow-file-access-from-files");
            startInfo.ArgumentList.Add($"--user-data-dir={Path.Combine(workDir, "profile")}");
            startInfo.ArgumentList.Add($"--print-to-pdf={pdfPath}");
            startInfo.ArgumentList.Add("--print-to-pdf-no-header");
            startInfo.ArgumentList.Add("--no-pdf-header-footer");
            startInfo.ArgumentList.Add(htmlPath);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start browser PDF renderer.");

            if (!process.WaitForExit(60000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("HTML report PDF rendering timed out.");
            }

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                _logger.LogWarning("Browser PDF renderer exited with {ExitCode}: {Error}", process.ExitCode, error);
                throw new InvalidOperationException("HTML report PDF renderer failed.");
            }

            if (!File.Exists(pdfPath))
            {
                throw new InvalidOperationException("HTML report PDF renderer did not create a PDF file.");
            }

            var bytes = File.ReadAllBytes(pdfPath);
            if (bytes.Length == 0)
            {
                throw new InvalidOperationException("HTML report PDF renderer created an empty PDF file.");
            }

            return bytes;
        }
        finally
        {
            try
            {
                Directory.Delete(workDir, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to delete temporary report PDF folder {WorkDir}.", workDir);
            }
        }
    }

    private static string? FindBrowserPath()
    {
        return BrowserPaths.FirstOrDefault(File.Exists);
    }
}

