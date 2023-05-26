using System;

namespace CommentsFeed.Models
{
    internal record EntityComments
    {
        public string Id { get; init; }
        public string[] Children { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; set; }
    }
}
