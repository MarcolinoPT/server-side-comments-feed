using System;

namespace CommentsFeed.Infrastructure
{
    public static class EntityId
    {
        public static string ToEntityId<T>(this Guid id)
        {
            return $"{typeof(T).Name}/{id}";
        }

        public static string ToEntityId<T>(this string id)
        {
            return $"{typeof(T).Name}/{id}";
        }

        public static Guid FromEntityId(this string id)
        {
            // index 0 is the type name
            // index 1 is the id itself
            const int index = 1;
            return Guid.Parse(id.Split('/')[index]);
        }
    }
}
