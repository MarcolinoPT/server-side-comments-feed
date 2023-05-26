using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using System;

namespace CommentsFeed.Infrastructure
{
    public class DocumentStoreHolder
    {
        // Use Lazy<IDocumentStore> to initialize the document store lazily
        // This ensures that it is created only once
        // - when first accessing the public `Store` property.
        private static readonly Lazy<IDocumentStore> _store = new(CreateStore);

        public IDocumentStore Store => _store.Value;

        private static IDocumentStore CreateStore()
        {
            IDocumentStore store = new DocumentStore
            {
                // Define the cluster node URLs (required)
                // Set proper URL for db in container
                Urls = new[] { "http://172.26.112.1:8080", 
                           /*some additional nodes of this cluster*/ },

                // Set conventions as necessary (optional)
                Conventions =
                {
                    MaxNumberOfRequestsPerSession = 10,
                    UseOptimisticConcurrency = true
                },

                // Define a default database (optional)
                Database = "CommentsFeed",

                // Initialize the Document Store
            }.Initialize();

            return store;
        }
    }

    public static class DocumentStoreHolderExtensions
    {
        public static IServiceCollection AddDocumentStoreHolder(this IServiceCollection services)
        {
            return services.AddSingleton<DocumentStoreHolder>();
        }
    }
}
