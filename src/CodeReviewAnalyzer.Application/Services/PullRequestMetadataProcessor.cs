using CodeReviewAnalyzer.Application.Integrations;
using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Models;
using CodeReviewAnalyzer.Application.Repositories;

namespace CodeReviewAnalyzer.Application.Services;

public class PullRequestMetadataProcessor(
    IPullRequestsClient pullRequestsClient,
    IConfigurations configurations,
    IDayOff dayOff,
    IUsers users,
    IPullRequests pullRequests)
{
    private readonly PeriodTimeSpan _morningWorkingTime = new(TimeSpan.FromHours(8), TimeSpan.FromHours(12));
    private readonly PeriodTimeSpan _afternoonWorkingTime = new(TimeSpan.FromHours(13), TimeSpan.FromHours(17));
    private readonly IPullRequestsClient _pullRequestsClient = pullRequestsClient;
    private readonly IConfigurations _configurations = configurations;
    private readonly IDayOff _dayOff = dayOff;
    private readonly IPullRequests _pullRequests = pullRequests;

    public async Task ExecuteAsync()
    {
        var configurations = await _configurations.GetAllAsync();
        var holidays = await _dayOff.GetAllAsync(
            from: new DateOnly(2024, 01, 01),
            to: DateOnly.FromDateTime(DateTime.Now));
        var calculator = new WorkingHourCalculator(
            _morningWorkingTime,
            _afternoonWorkingTime,
            holidays.Select(holiday => holiday.Date));

        foreach (var configuration in configurations)
        {
            await ProcessConfigurationAsync(configuration, calculator);
        }
    }

    private async Task ProcessConfigurationAsync(
        Configuration configuration,
        WorkingHourCalculator calculator)
    {

        var pullRequests = _pullRequestsClient.GetPullRequestsAsync(
            configuration,
            calculator,
            minTime: new DateTime(2024, 01, 01),
            maxTime: DateTime.Now);

        await foreach (var pullRequest in pullRequests)
        {
            await ProcessUserAsync(pullRequest);
            await ProcessPullRequestAsync(pullRequest);
        }
    }

    private async Task ProcessUserAsync(PullRequest pullRequest)
    {
        await users.Upsert(pullRequest.CreatedBy);
        foreach (var reviewer in pullRequest.Reviewers)
        {
            await users.Upsert(reviewer);
        }
    }

    private async Task ProcessPullRequestAsync(PullRequest pullRequest) =>
        await _pullRequests.Add(pullRequest);
}
