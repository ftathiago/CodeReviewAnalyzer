namespace CodeReviewAnalyzer.Application.Integrations.Models;

public class PrComments
{
    public required User CommentedBy { get; init; }

    public required string Comment { get; init; }

    public required DateTime CommentDate { get; init; }

    public required DateTime ResolvedDate { get; init; }
}
