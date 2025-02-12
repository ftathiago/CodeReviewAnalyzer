using CodeReviewAnalyzer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PullRequestCrawlerJobController(PullRequestMetadataProcessor metadataProcessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SchedulePullRequestCrawlingAsync(
        [FromQuery] DateOnly? begin,
        [FromQuery] DateOnly? end)
    {
        await Task.Delay(1);
        Task.Run(() => metadataProcessor.ExecuteAsync(
            begin: begin ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1),
            end: end ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(1).AddTicks(-1))));
        return Ok();
    }
}
