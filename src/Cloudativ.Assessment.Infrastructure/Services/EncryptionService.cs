using System.Security.Cryptography;
using System.Text;
using Cloudativ.Assessment.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<EncryptionService> _logger;
    private const string Purpose = "Cloudativ.Assessment.Secrets";

    public EncryptionService(IDataProtectionProvider dataProtectionProvider, ILogger<EncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            return _protector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        using var rfc2898 = new Rfc2898DeriveBytes(
            password,
            saltSize: 16,
            iterations: 100000,
            HashAlgorithmName.SHA256);

        var salt = rfc2898.Salt;
        var hash = rfc2898.GetBytes(32);

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            if (hashBytes.Length != 48)
                return false;

            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            using var rfc2898 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations: 100000,
                HashAlgorithmName.SHA256);

            var computedHash = rfc2898.GetBytes(32);

            for (int i = 0; i < 32; i++)
            {
                if (hashBytes[i + 16] != computedHash[i])
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
