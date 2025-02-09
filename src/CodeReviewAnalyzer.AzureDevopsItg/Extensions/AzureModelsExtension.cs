using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Models;
using CodeReviewAnalyzer.Application.Services;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.Json;

namespace CodeReviewAnalyzer.AzureDevopsItg.Extensions;

public static class AzureModelsExtension
{
    public static PullRequest ToPullRequest(
        this GitPullRequest gitPullRequest,
        Configuration configuration,
        WorkingHourCalculator calculator,
        List<GitPullRequestCommentThread> threads,
        int count = 0)
    {
        try
        {
            var approvals = threads.SelectMany(t => t.Comments)
                .Where(c =>
                    c.CommentType == CommentType.System && (
                    c.Content.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
                    c.Content.Contains("aprovado", StringComparison.OrdinalIgnoreCase) ||
                    c.Content.Contains("voted 10", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(c => c.PublishedDate);

            var personsComments = threads
                    .Where(thread => thread.Status != CommentThreadStatus.Unknown)
                    .SelectMany(t => t.Comments, (thread, comment) => new
                    {
                        Thread = thread,
                        Comment = comment,
                    })
                    .Where(x => !x.Comment.IsDeleted
                        && x.Comment.CommentType != CommentType.System
                        && x.Comment.CommentType != CommentType.CodeChange
                        && x.Comment.Author != null)
                    .OrderBy(x => x.Thread.PublishedDate);
            return new(calculator)
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
                FirstCommentDate = personsComments.FirstOrDefault()?.Thread.PublishedDate ?? gitPullRequest.CreationDate,
                LastApprovalDate = approvals.FirstOrDefault()?.PublishedDate ?? gitPullRequest.ClosedDate,
                Comments = personsComments
                    .Select(x => new PrComments
                    {
                        CommentIndex = x.Comment.Id,
                        ThreadId = x.Thread.Id,
                        CommentedBy = new User()
                        {
                            Id = x.Comment.Author.Id,
                            Name = x.Comment.Author.DisplayName,
                            Active = !x.Comment.Author.Inactive,
                        },
                        Comment = x.Comment.Content,
                        CommentDate = x.Comment.PublishedDate,
                        ResolvedDate = x.Comment.LastUpdatedDate,
                    }),
                ThreadCount = personsComments.Count(),
            };
        }
        catch (System.Exception)
        {
            Console.WriteLine(JsonSerializer.Serialize(gitPullRequest));
            throw;
        }
    }

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
