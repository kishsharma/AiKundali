using System.Security.Cryptography;
using System.Text;

namespace KundaliEngine.API.Services.ReportShare;

public static class EncryptedSecretProtector
{
    private const string Prefix = "v1";
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public static string Decrypt(string encryptedValue, string keyMaterial)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            throw new InvalidOperationException("SMTP password encryption key is not configured.");
        }

        var parts = encryptedValue.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Encrypted SMTP password format is invalid.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var cipherText = Convert.FromBase64String(parts[3]);
        var plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(DeriveKey(keyMaterial), TagSizeBytes);
        aes.Decrypt(nonce, cipherText, tag, plainText);
        return Encoding.UTF8.GetString(plainText);
    }

    public static string Encrypt(string plainText, string keyMaterial)
    {
        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            throw new InvalidOperationException("SMTP password encryption key is required.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var source = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        var cipherText = new byte[source.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(DeriveKey(keyMaterial), TagSizeBytes);
        aes.Encrypt(nonce, source, cipherText, tag);
        return string.Join(':', Prefix, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(cipherText));
    }

    private static byte[] DeriveKey(string keyMaterial)
    {
        var trimmed = keyMaterial.Trim();
        try
        {
            var decoded = Convert.FromBase64String(trimmed);
            if (decoded.Length == KeySizeBytes)
            {
                return decoded;
            }
        }
        catch (FormatException)
        {
            // A normal passphrase is allowed; it is hashed into the required AES-256 key size.
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(trimmed));
    }
}
