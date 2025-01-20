using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Repositories;
using CodeReviewAnalyzer.Database.Contexts;

namespace CodeReviewAnalyzer.Database.Repositories;

public class PullRequestsRepository : IPullRequests
{
    private const string InsertSql =
        """
            INSERT INTO "PULL_REQUEST" (
                  "EXTERNAL_ID"
                , "TITLE"
                , "REPOSITORY_NAME"
                , "CREATION_DATE"
                , "CLOSED_DATE"
                , "URL"
                , "CREATED_BY_ID"
                , "WAITING_TIME_MINUTES"
                , "FIRST_COMMENT_DATE"
                , "LAST_COMMENT_RESOLVED_DATE"
                , "MERGE_MODE"
                , "FILE_COUNT"
            ) VALUES(
                  @ExternalId
                , @Title
                , @RepositoryName
                , @CreationDate
                , @ClosedDate
                , @Url
                , (select u."ID" from "USERS" u where u."EXTERNAL_IDENTIFIER" = @UserKey)
                , @WaitingTimeMinutes
                , @FirstCommentDate
                , @LastCommentResolvedDate
                , @MergeMode
                , @FileCount) returning "ID";

        """;

    private const string InsertComments =
        """
            INSERT INTO "PULL_REQUEST_COMMENTS" (
                  "PULL_REQUEST_ID"
                , "USER_ID"
                , "COMMENT_DATE"
                , "COMMENT"
                , "RESOLVED_DATE"
            ) VALUES (
                  @PullRequestId
                , (select u."ID" from "USERS" u where u."EXTERNAL_IDENTIFIER" = @UserId)
                , @CommentDate
                , @Comment
                , @ResolvedDate
            );

        """;

    private readonly IDatabaseFacade _databaseFacade;

    public PullRequestsRepository(IDatabaseFacade databaseFacade)
    {
        _databaseFacade = databaseFacade;
    }

    public async Task Add(PullRequest pullRequest)
    {
        var id = await _databaseFacade.ExecuteScalarAsync<int>(InsertSql, new
        {
            ExternalId = pullRequest.Id.ToString(),
            pullRequest.Title,
            pullRequest.RepositoryName,
            pullRequest.CreationDate,
            pullRequest.ClosedDate,
            pullRequest.Url,
            UserKey = pullRequest.CreatedBy.Id,
            WaitingTimeMinutes = pullRequest.WaitingTime.TotalMinutes,
            pullRequest.FirstCommentDate,
            pullRequest.LastCommentResolvedDate,
            pullRequest.MergeMode,
            pullRequest.FileCount,
        });
        foreach (var comment in pullRequest.Comments)
        {
            await _databaseFacade.ExecuteAsync(
            InsertComments,
            new
            {
                PullRequestId = id,
                UserId = pullRequest.CreatedBy.Id,
                comment.CommentDate,
                comment.Comment,
                comment.ResolvedDate,
            });
        }
    }
}
