namespace TeamHubConnect.Application.Common.Interfaces;

public interface IEncryptionService
{
    Task<EncryptionResult> EncryptAsync(string plainText, string? publicKey = null, CancellationToken cancellationToken = default);
    Task<DecryptionResult> DecryptAsync(string encryptedText, string key, string iv, string? privateKey = null, CancellationToken cancellationToken = default);
    Task<KeyPairResult> GenerateKeyPairAsync(CancellationToken cancellationToken = default);
    Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default);
    Task<string> GenerateSecureTokenAsync(int length = 32, CancellationToken cancellationToken = default);
    Task<SignatureResult> SignDataAsync(string data, string privateKey, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(string data, string signature, string publicKey, CancellationToken cancellationToken = default);
}

public class EncryptionResult
{
    public string EncryptedData { get; set; } = "";
    public string Key { get; set; } = "";
    public string IV { get; set; } = "";
    public string Algorithm { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DecryptionResult
{
    public string PlainText { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class KeyPairResult
{
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string Algorithm { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SignatureResult
{
    public string Signature { get; set; } = "";
    public string Algorithm { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}