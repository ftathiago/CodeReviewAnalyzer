using CodeReviewAnalyzer.Application.Services;

namespace CodeReviewAnalyzer.Application.Integrations.Models;

public class PullRequest(WorkingHourCalculator workingHourCalculator)
{
    private readonly WorkingHourCalculator _workingHourCalculator = workingHourCalculator;

    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string RepositoryName { get; init; }

    public required DateTime CreationDate { get; init; }

    public required DateTime ClosedDate { get; init; }

    public required string Url { get; init; }

    public TimeSpan PlainWaitingTime => ClosedDate.Subtract(CreationDate);

    public TimeSpan WaitingTime => _workingHourCalculator.Calculate(CreationDate, ClosedDate);

    public required User CreatedBy { get; init; }

    public required int FileCount { get; init; }

    public required DateTime FirstCommentDate { get; init; }

    public required DateTime LastCommentResolvedDate { get; init; }

    public required string MergeMode { get; init; }

    public required IEnumerable<User> Reviewers { get; init; }

    public required IEnumerable<PrComments> Comments { get; init; }
}
