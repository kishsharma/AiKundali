namespace KundaliEngine.API.Services.Billing;

public static class DotEnvLoader
{
    public static void Load(params string[] searchDirectories)
    {
        var directories = new List<string>();
        foreach (var directory in searchDirectories)
        {
            AddDirectoryAndParents(directories, directory);
        }

        AddDirectoryAndParents(directories, AppContext.BaseDirectory);
        AddDirectoryAndParents(directories, Directory.GetCurrentDirectory());

        foreach (var directory in directories)
        {
            var path = Path.Combine(directory, ".env");
            if (File.Exists(path))
            {
                LoadFile(path);
                return;
            }
        }
    }

    private static void AddDirectoryAndParents(ICollection<string> directories, string? directory)
    {
        var current = directory;
        for (var depth = 0; depth < 5 && !string.IsNullOrWhiteSpace(current); depth++)
        {
            if (!directories.Contains(current))
            {
                directories.Add(current);
            }

            current = Directory.GetParent(current)?.FullName;
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"').Trim('\'');
            if (key.Length == 0 || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static IDictionary<string, string?> GetRazorpayConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Razorpay:KeyId"] = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID"),
                Environment.GetEnvironmentVariable("Razorpay__KeyId")),
            ["Razorpay:KeySecret"] = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET"),
                Environment.GetEnvironmentVariable("Razorpay__KeySecret")),
            ["Razorpay:WebhookSecret"] = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_WEBHOOK_SECRET"),
                Environment.GetEnvironmentVariable("Razorpay__WebhookSecret"))
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
