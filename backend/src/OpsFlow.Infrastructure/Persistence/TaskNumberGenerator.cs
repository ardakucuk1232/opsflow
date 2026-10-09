using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Infrastructure.Persistence;

public sealed class TaskNumberGenerator : ITaskNumberGenerator
{
    private readonly OpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TaskNumberGenerator(OpsFlowDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<int> NextAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId is not Guid companyId)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var numbers = await _db.Database
            .SqlQuery<int>($"""
                UPDATE "Projects"
                SET "LastTaskNumber" = "LastTaskNumber" + 1
                WHERE "Id" = {projectId} AND "CompanyId" = {companyId} AND NOT "IsDeleted"
                RETURNING "LastTaskNumber" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return numbers.Count == 1
            ? numbers[0]
            : throw new NotFoundException("The project was not found.");
    }
}
