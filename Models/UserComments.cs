using System;
using System.Collections.Generic;

namespace CommentsFeed.Models
{
    public record UserComments
    {
        public string Id { get; set; }
        public Dictionary<string, string> LastViewed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
    }
}
