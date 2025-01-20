using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Models;
using CodeReviewAnalyzer.Application.Services;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace CodeReviewAnalyzer.AzureDevopsItg.Extensions;

public static class AzureModelsExtension
{
    public static PullRequest ToPullRequest(
        this GitPullRequest gitPullRequest,
        Configuration configuration,
        WorkingHourCalculator calculator,
        List<GitPullRequestCommentThread> threads,
        int count = 0) => new(calculator)
        {
            Id = gitPullRequest.PullRequestId,
            Title = gitPullRequest.Title,
            RepositoryName = gitPullRequest.Repository.Name,
            CreationDate = gitPullRequest.CreationDate,
            ClosedDate = gitPullRequest.ClosedDate,
            Url = $"https://dev.azure.com/{configuration.Organization}/{configuration.ProjectName}/_git/{gitPullRequest.Repository.Name}/pullrequest/{gitPullRequest.PullRequestId}",
            CreatedBy = gitPullRequest.CreatedBy.ToUser(),
            FileCount = count,
            MergeMode = gitPullRequest.CompletionOptions?.MergeStrategy != null
                ? Enum.GetName(typeof(GitPullRequestMergeStrategy), gitPullRequest.CompletionOptions.MergeStrategy) ?? "Unknown"
                : "Unknown",
            Reviewers = gitPullRequest.Reviewers
                .Aggregate(
                    new List<User>(),
                    (users, reviewer) =>
                    {
                        if (!reviewer.Inactive)
                        {
                            users.Add(reviewer.ToUser());
                        }

                        return users;
                    }),
            FirstCommentDate = threads
                .SelectMany(t => t.Comments)
                .Where(comment => !comment.IsDeleted
                    && comment.CommentType != CommentType.System
                    && comment.CommentType != CommentType.CodeChange)
                .Select(c => c.PublishedDate)
                .DefaultIfEmpty(gitPullRequest.CreationDate)
                .Min(),
            LastCommentResolvedDate = threads
                .Where(thread => thread.Status == CommentThreadStatus.Closed)
                .SelectMany(t => t.Comments)
                .Where(comment => !comment.IsDeleted
                    && comment.CommentType != CommentType.System
                    && comment.CommentType != CommentType.CodeChange)
                .Select(c => c.LastUpdatedDate)
                .DefaultIfEmpty(gitPullRequest.ClosedDate)
                .Max(),
            Comments = threads
                .Where(thread => thread.Status == CommentThreadStatus.Closed || thread.Status == CommentThreadStatus.Fixed)
                .SelectMany(t => t.Comments)
                .Where(comment => !comment.IsDeleted
                    && comment.CommentType != CommentType.System
                    && comment.CommentType != CommentType.CodeChange)

                // Só está pegando o autor da thread, não o autor do comentário
                .Select(comment => new PrComments
                {
                    CommentedBy = new User()
                    {
                        Id = comment.Author.Id,
                        Name = comment.Author.DisplayName,
                        Active = !comment.Author.Inactive,
                    },
                    Comment = comment.Content,
                    CommentDate = comment.PublishedDate,
                    ResolvedDate = comment.LastUpdatedDate,
                }),
        };

    public static User ToUser(this IdentityRef identifyRef) => new()
    {
        Id = identifyRef.Id,
        Name = identifyRef.DisplayName,
        Active = !identifyRef.Inactive,
    };

    public static User ToUser(this IdentityRefWithVote identityRef) => new()
    {
        Id = identityRef.Id,
        Name = identityRef.DisplayName,
        Active = !identityRef.Inactive,
    };
}
