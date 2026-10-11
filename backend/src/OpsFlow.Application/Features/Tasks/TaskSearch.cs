using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Persistence;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Features.Tasks;

public static partial class TaskSearch
{
    public static IQueryable<TaskItem> Matching(this IQueryable<TaskItem> tasks, string term)
    {
        term = term.Trim();

        var key = TaskKey().Match(term);

        if (key.Success && int.TryParse(key.Groups["number"].Value, out var number))
        {
            var projectKey = key.Groups["project"].Value.ToUpperInvariant();

            return tasks.Where(t => t.Project.Key == projectKey && t.Number == number);
        }

        var pattern = SqlText.ContainsPattern(term);

        return tasks.Where(t => EF.Functions.Like(t.Title.ToLower(), SqlText.Lower(pattern), SqlText.EscapeCharacter));
    }

    [GeneratedRegex(@"^(?<project>[A-Za-z][A-Za-z0-9]{1,9})-(?<number>\d{1,9})$")]
    private static partial Regex TaskKey();
}
