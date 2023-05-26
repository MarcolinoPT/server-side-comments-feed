using CommentsFeed.Infrastructure;
using CommentsFeed.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommentsFeed.Features.Comments
{
    [Route("api/v1/comments")]
    public class Read : ControllerBase
    {
        private readonly IMediator _mediator;

        public Read(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Handle([FromQuery] ReadCommentsRequest request,
                                                CancellationToken cancellationToken = new())
        {
            var comments = await _mediator.Send(new Query(request),
                                                cancellationToken);
            await _mediator.Publish(new QueryNotification(request.EntityId,
                                                          request.UserId,
                                                          comments.Comments.LastOrDefault()?.Id),
                                                          cancellationToken);
            return Ok(comments);
        }

        public record ReadCommentsRequest
        {
            public string EntityId { get; init; }
            public string UserId { get; init; }
            public int PageIndex { get; init; }
            // To match the default page size of RavenDB
            public int PageSize { get; init; } = 25;
            public bool Newer { get; init; }
        }

        public record ReadCommentsResponse
        {
            public CommentResponse[] Comments { get; init; }
            public int Count { get; init; }
            public int PageIndex { get; init; }
        }

        public record CommentResponse
        {
            public string Id { get; init; }
            public string AuthorId { get; init; }
            public DateTime CreatedAt { get; init; }
            public string Content { get; init; }
        }

        internal record Query : IRequest<ReadCommentsResponse>
        {
            public Query(ReadCommentsRequest request)
            {
                EntityId = request.EntityId;
                UserId = request.UserId;
                PageIndex = request.PageIndex;
                PageSize = request.PageSize;
                Newer = request.Newer;
            }

            public string EntityId { get; }
            public string UserId { get; }
            public int PageIndex { get; }
            public int PageSize { get; }
            public bool Newer { get; }
        }

        internal class Handler : IRequestHandler<Query, ReadCommentsResponse>
        {
            private readonly DocumentStoreHolder _storeHolder;

            public Handler(DocumentStoreHolder storeHolder)
            {
                _storeHolder = storeHolder;
            }

            public async Task<ReadCommentsResponse> Handle(Query request,
                                                           CancellationToken cancellationToken)
            {
                using IAsyncDocumentSession session = _storeHolder.Store.OpenAsyncSession();
                var entityComments = await session.LoadAsync<EntityComments>(id: request.EntityId.ToEntityId<EntityComments>(),
                                                                             token: cancellationToken);
                var skipCount = request.PageIndex * request.PageSize;
                if (request.Newer
                    && entityComments is not null)
                {
                    var userComments = await session.LoadAsync<UserComments>(id: request.UserId.ToEntityId<UserComments>(),
                                                                             token: cancellationToken);
                    var lastCommentViewedByUser = userComments?.LastViewed[request.EntityId];
                    skipCount = Array.IndexOf(array: entityComments.Children,
                                              value: lastCommentViewedByUser.ToEntityId<Comment>()) + 1;
                }
                // Get the children based on the skip count
                // or return an empty array if there is no matching entity
                var children = entityComments?.Children.Skip(skipCount)
                                                       .Take(request.PageSize)
                               ?? Array.Empty<string>();
                var comments = await session.LoadAsync<Comment>(ids: children,
                                                                token: cancellationToken);
                return new ReadCommentsResponse
                {
                    Comments = comments.Values.Select(comment => new CommentResponse
                    {
                        Id = comment.Id.FromEntityId(),
                        AuthorId = comment.UserId,
                        CreatedAt = comment.CreatedAt,
                        Content = comment.Content
                    }).ToArray(),
                    Count = comments.Count,
                    PageIndex = request.PageIndex
                };
            }
        }

        internal record QueryNotification(string EntityId,
                                          string UserId,
                                          string LastCommentId) : INotification;

        internal class QueryNotificationHandler : INotificationHandler<QueryNotification>
        {
            private readonly DocumentStoreHolder _storeHolder;

            public QueryNotificationHandler(DocumentStoreHolder storeHolder)
            {
                _storeHolder = storeHolder;
            }

            public async Task Handle(QueryNotification notification,
                                     CancellationToken cancellationToken)
            {
                // Skip if there is no last comment
                if (notification.LastCommentId is null)
                {
                    return;
                }
                // Store the last viewed comment for the user
                using IAsyncDocumentSession session = _storeHolder.Store.OpenAsyncSession();
                var userComments = await session.LoadAsync<UserComments>(id: notification.UserId.ToEntityId<UserComments>(),
                                                                         token: cancellationToken);
                // New views create a new record
                if (userComments is null)
                {
                    await session.StoreAsync(new UserComments
                    {
                        Id = notification.UserId.ToEntityId<UserComments>(),
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        LastViewed = new()
                        {
                            [notification.EntityId] = notification.LastCommentId
                        }
                    }, cancellationToken);
                }
                // Existing views update the existing record
                else
                {
                    userComments.LastViewed[notification.EntityId] = notification.LastCommentId;
                    userComments.LastModified = DateTime.UtcNow;
                    await session.StoreAsync(userComments, cancellationToken);
                }
                await session.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
