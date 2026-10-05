using BCrypt.Net;
using OpsFlow.Application.Common.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace OpsFlow.Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        return BCryptNet.EnhancedHashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCryptNet.EnhancedVerify(password, passwordHash);
        }

        catch (SaltParseException)
        {
            return false;
        }
    }
}