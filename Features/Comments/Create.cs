using CommentsFeed.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;
using System;
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
    }

    public record CreateCommentRequest
    {
        public string EntityId { get; init; }
        public string Content { get; init; }
        public string AuthorId { get; init; }
    }

    internal record Command : IRequest
    {
        public Command(CreateCommentRequest request)
        {
            Request = request;
        }

        public CreateCommentRequest Request { get; init; }
    }

    // I use entity explicitly on name on purpose
    internal record CommentEntity
    {
        public Guid EntityId { get; init; }
        public Guid UserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public string Content { get; init; }
    }

    internal class Handler : IRequestHandler<Command>
    {
        private readonly DocumentStoreHolder _storeHolder;

        public Handler(DocumentStoreHolder storeHolder)
        {
            _storeHolder = storeHolder;
        }

        public async Task Handle(Command request,
                                       CancellationToken cancellationToken)
        {
            using IAsyncDocumentSession session = _storeHolder.Store.OpenAsyncSession();
            await session.StoreAsync(new CommentEntity
            {
                Content = request.Request.Content,
                CreatedAt = DateTime.Now,
                EntityId = Guid.Parse(request.Request.EntityId),
                UserId = Guid.Parse(request.Request.AuthorId)
            }, cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
            return;
        }
    }
}
