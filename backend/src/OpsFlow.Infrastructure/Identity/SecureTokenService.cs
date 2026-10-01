using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Infrastructure.Indentity;

public sealed class SecureTokenService : ISecureTokenService
{
    private const int TokenSizeInBytes = 64;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);

        return Base64Url.EncodeToString(bytes);
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexStringLower(hash);
    }
}