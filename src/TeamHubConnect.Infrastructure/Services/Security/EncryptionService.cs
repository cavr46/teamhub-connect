using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Infrastructure.Services.Security;

public class EncryptionService : IEncryptionService
{
    private readonly EncryptionOptions _options;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        _options = options.Value;
    }

    public async Task<EncryptionResult> EncryptAsync(string plainText, string? publicKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            await swEncrypt.WriteAsync(plainText.AsMemory(), cancellationToken);
            
            var encrypted = msEncrypt.ToArray();
            var encryptedText = Convert.ToBase64String(encrypted);
            var keyBase64 = Convert.ToBase64String(aes.Key);
            var ivBase64 = Convert.ToBase64String(aes.IV);

            // If public key is provided, encrypt the AES key with RSA
            string encryptedKey = keyBase64;
            if (!string.IsNullOrEmpty(publicKey))
            {
                encryptedKey = await EncryptKeyWithRSAAsync(aes.Key, publicKey, cancellationToken);
            }

            return new EncryptionResult
            {
                EncryptedData = encryptedText,
                Key = encryptedKey,
                IV = ivBase64,
                Algorithm = "AES-256-CBC",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new EncryptionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DecryptionResult> DecryptAsync(string encryptedText, string key, string iv, string? privateKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var encrypted = Convert.FromBase64String(encryptedText);
            var ivBytes = Convert.FromBase64String(iv);
            
            byte[] keyBytes;
            if (!string.IsNullOrEmpty(privateKey))
            {
                // Decrypt AES key with RSA private key
                keyBytes = await DecryptKeyWithRSAAsync(key, privateKey, cancellationToken);
            }
            else
            {
                keyBytes = Convert.FromBase64String(key);
            }

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            
            using var msDecrypt = new MemoryStream(encrypted);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            var plainText = await srDecrypt.ReadToEndAsync(cancellationToken);

            return new DecryptionResult
            {
                PlainText = plainText,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new DecryptionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<KeyPairResult> GenerateKeyPairAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var rsa = RSA.Create(4096);
            
            var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

            return new KeyPairResult
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                Algorithm = "RSA-4096",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new KeyPairResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Password hashing failed", ex);
        }
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateSecureTokenAsync(int length = 32, CancellationToken cancellationToken = default)
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Token generation failed", ex);
        }
    }

    public async Task<SignatureResult> SignDataAsync(string data, string privateKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return new SignatureResult
            {
                Signature = Convert.ToBase64String(signature),
                Algorithm = "RSA-SHA256",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new SignatureResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> VerifySignatureAsync(string data, string signature, string publicKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> EncryptKeyWithRSAAsync(byte[] key, string publicKey, CancellationToken cancellationToken)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        var encryptedKey = rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedKey);
    }

    private async Task<byte[]> DecryptKeyWithRSAAsync(string encryptedKey, string privateKey, CancellationToken cancellationToken)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        var encryptedKeyBytes = Convert.FromBase64String(encryptedKey);
        return rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);
    }
}

public class EncryptionOptions
{
    public string DefaultAlgorithm { get; set; } = "AES-256-CBC";
    public int KeySize { get; set; } = 256;
    public int RSAKeySize { get; set; } = 4096;
    public bool EnableE2EEncryption { get; set; } = false;
}