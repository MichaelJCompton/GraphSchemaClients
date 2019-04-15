using System;
using System.Net.Http;
using GraphSchema.io.Client;
using GraphSchema.io.Client.Resources;

namespace Microsoft.Extensions.DependencyInjection {

    public static class GraphSchemIOClientDependencyInjectionExtensions {
        public static IServiceCollection AddGraphSchemaIOLClient(
            this IServiceCollection services,
            Action<HttpClient> configureHttpClient
        ) {
            services.AddGraphQLClient<IGraphSchemaIOClient, GraphSchemaIOClient>(
                configureHttpClient, 
                ResourceProvider.GetSchemaFragment(),
                new [] { "GraphSchema.io.Client.Models" });

            return services;
        }
    }

}