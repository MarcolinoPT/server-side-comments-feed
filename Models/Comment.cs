using System;

namespace CommentsFeed.Features.Comments
{
    internal record Comment
    {
        public string Id { get; init; }
        public string UserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public string Content { get; init; }
    }
}
