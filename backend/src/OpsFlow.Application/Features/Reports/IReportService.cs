namespace OpsFlow.Application.Features.Reports;

public interface IReportService
{
    Task<ReportOverviewDto> OverviewAsync(ReportQuery query, CancellationToken cancellationToken);

    Task<MySummaryDto> MySummaryAsync(DateOnly? today, CancellationToken cancellationToken);
}
