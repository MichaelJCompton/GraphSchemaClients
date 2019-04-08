using System.Net.Http;
using GraphSchema.io.Client;
using GraphSchema.io.Client.Resources;

namespace Microsoft.Extensions.DependencyInjection {

    public static class GraphSchemIOClientDependencyInjectionExtensions {
        public static IServiceCollection AddGraphSchemaIOLClient(
            this IServiceCollection services,
            HttpClient httpClient
        ) {
            services.AddGraphQLClient<IGraphSchemaIOClient, GraphSchemaIOClient>(
                httpClient, 
                ResourceProvider.GetSchemaFragment(),
                new [] { "GraphSchema.io.Client.Models" });

            return services;
        }
    }

}