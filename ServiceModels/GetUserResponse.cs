using System.Collections.Generic;
using CommentsFeed.Dtos;

namespace CommentsFeed.ServiceModels
{
    public class GetUserResponse
    {
        public Dictionary<int, UserDto> Users { get; set; }
    }
}