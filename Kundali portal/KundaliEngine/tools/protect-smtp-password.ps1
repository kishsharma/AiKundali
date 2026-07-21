param(
    [Parameter(Mandatory = $true)]
    [string]$Password,

    [string]$Key = $env:KUNDALI_SMTP_PASSWORD_KEY
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Key)) {
    throw "Set KUNDALI_SMTP_PASSWORD_KEY first, or pass -Key."
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("kundali-secret-protect-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null

try {
    $projectPath = Join-Path $work "SecretProtect.csproj"
    $programPath = Join-Path $work "Program.cs"

    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath $projectPath -Encoding UTF8

    @'
using System.Security.Cryptography;
using System.Text;

const string prefix = "v1";
const int keySizeBytes = 32;
const int nonceSizeBytes = 12;
const int tagSizeBytes = 16;

if (args.Length < 2)
{
    throw new InvalidOperationException("Password and key are required.");
}

var plainText = args[0];
var keyMaterial = args[1];
var key = DeriveKey(keyMaterial);
var nonce = RandomNumberGenerator.GetBytes(nonceSizeBytes);
var source = Encoding.UTF8.GetBytes(plainText);
var cipherText = new byte[source.Length];
var tag = new byte[tagSizeBytes];

using var aes = new AesGcm(key, tagSizeBytes);
aes.Encrypt(nonce, source, cipherText, tag);
Console.WriteLine(string.Join(':', prefix, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(cipherText)));

static byte[] DeriveKey(string keyMaterial)
{
    var trimmed = keyMaterial.Trim();
    try
    {
        var decoded = Convert.FromBase64String(trimmed);
        if (decoded.Length == keySizeBytes)
        {
            return decoded;
        }
    }
    catch (FormatException)
    {
    }

    return SHA256.HashData(Encoding.UTF8.GetBytes(trimmed));
}
'@ | Set-Content -LiteralPath $programPath -Encoding UTF8

    dotnet run --project $projectPath -- $Password $Key
}
finally {
    if (Test-Path -LiteralPath $work) {
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}