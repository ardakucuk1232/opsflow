using OpsFlow.Application.Common.Models;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user, IReadOnlyCollection<string> roles);
}