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
    public class Create : ControllerBase
    {
        private readonly IMediator _mediator;

        public Create(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] CreateCommentRequest request,
                                                CancellationToken cancellationToken = new())
        {
            await _mediator.Send(new Command(request),
                                 cancellationToken);
            return NoContent();
        }

        public record CreateCommentRequest
        {
            public string EntityId { get; init; }
            public string AuthorId { get; init; }
            public string Content { get; init; }
        }

        internal record Command : IRequest
        {
            public Command(CreateCommentRequest request)
            {
                EntityId = request.EntityId;
                Content = request.Content;
                AuthorId = request.AuthorId;
            }

            public string EntityId { get; init; }
            public string Content { get; init; }
            public string AuthorId { get; init; }
        }

        internal class Handler : IRequestHandler<Command>
        {
            private readonly DocumentStoreHolder _storeHolder;

            public Handler(DocumentStoreHolder storeHolder)
            {
                _storeHolder = storeHolder;
            }

            public async Task Handle(Command command,
                                     CancellationToken cancellationToken)
            {
                using IAsyncDocumentSession session = _storeHolder.Store.OpenAsyncSession();
                // Store new comment so we can relate to the entity
                var newComment = new Comment
                {
                    Id = Guid.NewGuid().ToEntityId<Comment>(),
                    Content = command.Content,
                    CreatedAt = DateTime.Now,
                    EntityId = command.EntityId,
                    UserId = command.AuthorId
                };
                await session.StoreAsync(entity: newComment,
                                         id: newComment.Id,
                                         token: cancellationToken);
                await session.SaveChangesAsync(cancellationToken);
                // Find the parent, if it exists
                var entityCommentsFound = await session.LoadAsync<EntityComments>(id: command.EntityId.ToEntityId<EntityComments>(),
                                                                                  token: cancellationToken);
                // Update the parent with the new comment reference
                if (entityCommentsFound is not null)
                {
                    entityCommentsFound.Children = entityCommentsFound.Children.Append(newComment.Id)
                                                                               .ToArray();
                    entityCommentsFound.UpdatedAt = DateTime.Now;
                }
                // Store the new parent if it doesn't exist
                else
                {
                    await session.StoreAsync(entity: new EntityComments
                    {
                        Id = newComment.EntityId.ToEntityId<EntityComments>(),
                        Children = new[] { newComment.Id },
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                                             token: cancellationToken);
                }
                await session.SaveChangesAsync(cancellationToken);
                return;
            }
        }
    }
}
