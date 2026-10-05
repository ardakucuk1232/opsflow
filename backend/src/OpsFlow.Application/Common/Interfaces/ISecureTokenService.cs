namespace OpsFlow.Application.Common.Interfaces;

public interface ISecureTokenService
{
    string GenerateToken();
    string HashToken(string token);
}