using CodeReviewAnalyzer.Application.Integrations;
using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Models;
using CodeReviewAnalyzer.Application.Services;
using CodeReviewAnalyzer.AzureDevopsItg.Extensions;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace CodeReviewAnalyzer.AzureDevopsItg.Clients.Impl;

internal sealed class PullRequestClient(IConnectionFactory connectionFactory) : IPullRequestsClient
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;

    public async IAsyncEnumerable<PullRequest> GetPullRequestsAsync(
        Configuration configuration,
        WorkingHourCalculator workingHourCalculator,
        DateTime? minTime,
        DateTime? maxTime = null)
    {
        using var connection = _connectionFactory.CreateConnection(configuration);

        var projectClient = await connection.GetClientAsync<ProjectHttpClient>();
        var project = await projectClient.GetProject(configuration.ProjectName);

        var gitClient = await connection.GetClientAsync<GitHttpClient>();

        var repositories = await gitClient.GetRepositoriesAsync(project.Id, includeHidden: false);

        foreach (var repository in repositories)
        {
            if (repository.IsDisabled ?? false)
            {
                continue;
            }

            var prs = await gitClient.GetPullRequestsAsync(
                repository.Id,
                new GitPullRequestSearchCriteria()
                {
                    Status = PullRequestStatus.Completed,
                    TargetRefName = "refs/heads/develop",
                    MinTime = minTime,
                    MaxTime = maxTime,
                });

            foreach (var pr in prs)
            {
                var count = 0;
                var threads = new List<GitPullRequestCommentThread>();

                var requests = new List<Task>
                {
                    Task.Run(async () => count = await CountChangedFilesAsync(gitClient, repository, pr)),
                    Task.Run(async () => threads = await gitClient.GetThreadsAsync(repository.Id, pr.PullRequestId))
                };

                await Task.WhenAll(requests);

                yield return pr.ToPullRequest(
                    configuration,
                    workingHourCalculator,
                    threads,
                    count);
            }
        }
    }

    private static async Task<int> CountChangedFilesAsync(
        GitHttpClient gitClient,
        GitRepository repository,
        GitPullRequest pr)
    {
        var iterations = await gitClient.GetPullRequestIterationsAsync(repository.Id, pr.PullRequestId);
        var lastIteration = iterations.LastOrDefault();
        if (lastIteration?.Id is not null)
        {
            var iterationChanges = await gitClient.GetPullRequestIterationChangesAsync(
                repository.Id.ToString(),
                pr.PullRequestId,
                lastIteration.Id.Value);
            return iterationChanges.ChangeEntries.Count();
        }

        return 0;
    }
}
