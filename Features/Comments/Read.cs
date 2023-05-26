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
            return Ok(comments);
        }

        public record ReadCommentsRequest
        {
            public Guid EntityId { get; init; }
        }

        public record ReadCommentsResponse
        {
            public CommentResponse[] Comments { get; init; }
            public int Count { get; init; }
        }

        public record CommentResponse
        {
            public Guid Id { get; init; }
            public Guid AuthorId { get; init; }
            public DateTime CreatedAt { get; init; }
            public string Content { get; init; }
        }

        internal record Query : IRequest<ReadCommentsResponse>
        {
            public Query(ReadCommentsRequest request)
            {
                EntityId = request.EntityId;
            }

            public Guid EntityId { get; }
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
                var comments = await session.LoadAsync<Comment>(ids: entityComments?.Children
                                                                     ?? Array.Empty<string>(),
                                                                token: cancellationToken);
                return new ReadCommentsResponse
                {
                    Comments = comments.Values.Select(comment => new CommentResponse
                    {
                        Id = comment.Id.FromEntityId(),
                        AuthorId = Guid.Parse(comment.UserId),
                        CreatedAt = comment.CreatedAt,
                        Content = comment.Content
                    }).ToArray(),
                    Count = comments.Count
                };
            }
        }
    }
}
