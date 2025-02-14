using CodeReviewAnalyzer.Application.Integrations.Models;
using CodeReviewAnalyzer.Application.Repositories;
using CodeReviewAnalyzer.Database.Contexts;

namespace CodeReviewAnalyzer.Database.Repositories;

public class PullRequestsRepository(IDatabaseFacade databaseFacade) : IPullRequests
{
    private const string InsertSql =
        """
            INSERT INTO "PULL_REQUEST" (
                  "EXTERNAL_ID"
                , "TITLE"
                , "CREATED_BY_ID"
                , "REPOSITORY_NAME"
                , "URL"
                , "CREATION_DATE"
                , "CLOSED_DATE"
                , "FIRST_COMMENT_DATE"
                , "LAST_APPROVAL_DATE"
                , "REVISION_WAITING_TIME_MINUTES"
                , "MERGE_WAITING_TIME_MINUTES"
                , "FIRST_COMMENT_WAITING_TIME_MINUTES"
                , "MERGE_MODE"
                , "FILE_COUNT"
                , "THREAD_COUNT"
            ) VALUES(
                  @ExternalId
                , @Title
                , (select u."ID" from "USERS" u where u."EXTERNAL_IDENTIFIER" = @UserKey)
                , @RepositoryName
                , @Url
                , @CreationDate
                , @ClosedDate
                , @FirstCommentDate
                , @LastApprovalDate
                , @RevisionWaitingTimeMinutes
                , @MergeWaitingTimeMinutes
                , @FirstCommentWaitingTimeMinutes
                , @MergeMode
                , @FileCount
                , @ThreadCount) returning "ID";

        """;

    private const string InsertComments =
        """
            INSERT INTO "PULL_REQUEST_COMMENTS" (
                  "PULL_REQUEST_ID"
                , "USER_ID"
                , "COMMENT_INDEX"
                , "THREAD_ID"
                , "COMMENT_DATE"
                , "COMMENT"
                , "RESOLVED_DATE"
            ) VALUES (
                  @PullRequestId
                , (select u."ID" from "USERS" u where u."EXTERNAL_IDENTIFIER" = @UserId)
                , @CommentIndex
                , @ThreadId
                , @CommentDate
                , @Comment
                , @ResolvedDate
            );

        """;

    public async Task Add(PullRequest pullRequest)
    {
        var id = await databaseFacade.ExecuteScalarAsync<int>(InsertSql, new
        {
            ExternalId = pullRequest.Id.ToString(),
            pullRequest.Title,
            pullRequest.RepositoryName,
            CreationDate = pullRequest.CreationDate.ToLocalTime(),
            ClosedDate = pullRequest.ClosedDate.ToLocalTime(),
            pullRequest.Url,
            UserKey = pullRequest.CreatedBy.Id,
            RevisionWaitingTimeMinutes = pullRequest.RevisionWaitingTime.TotalMinutes,
            MergeWaitingTimeMinutes = pullRequest.MergeWaitingTime.TotalMinutes,
            FirstCommentWaitingTimeMinutes = pullRequest.FirstCommentWaitingTime.TotalMinutes,
            FirstCommentDate = pullRequest.FirstCommentDate.ToLocalTime(),
            LastApprovalDate = pullRequest.LastApprovalDate.ToLocalTime(),
            pullRequest.MergeMode,
            pullRequest.FileCount,
            pullRequest.ThreadCount,
        });
        foreach (var comment in pullRequest.Comments)
        {
            await databaseFacade.ExecuteAsync(
            InsertComments,
            new
            {
                PullRequestId = id,
                UserId = comment.CommentedBy.Id,
                comment.CommentIndex,
                comment.ThreadId,
                CommentDate = comment.CommentDate.ToLocalTime(),
                comment.Comment,
                ResolvedDate = comment.ResolvedDate.ToLocalTime(),
            });
        }
    }
}
