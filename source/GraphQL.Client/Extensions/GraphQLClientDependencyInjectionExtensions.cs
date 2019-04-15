using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using FluentResults;
using GraphQL.Client;
using GraphQL.Client.Extensions;
using GraphQL.Client.Requests;
using GraphQL.Client.Schema;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection {
    public static class GraphQLClientDependencyInjectionExtensions {

        public static IServiceCollection AddGraphQLClient<TClientInterface, TClient>(
            this IServiceCollection services,
            Action<HttpClient> configureHttpClient,
            string schema,
            IEnumerable<string> modelNamespaces = null)
        where TClientInterface : class, IGraphQLClient where TClient : class, TClientInterface {

            services.AddTransient<IFieldValidator, FieldValidator>();
            services.AddTransient<ISchemaValidator, SchemaValidator>();

            var typeValidator = new TypeValidator(modelNamespaces ?? new List<string>());
            services.AddSingleton<ITypeValidator>(typeValidator);

            services.AddTransient<IRequestBuilder, RequestBuilder>();
            services.AddTransient<IResultBuilder, ResultBuilder>();

            services.AddHttpClient<IGraphQLRequestExecutor, HttpGraphQLRequestExecutor>(configureHttpClient);

            var schemaPrivider = new PartialSchemaProvider();
            schemaPrivider.WithSchema(schema);
            services.AddSingleton<IPartialSchemaProvider>(schemaPrivider);

            services.AddTransient<TClientInterface, TClient>();

            return services;
        }

    }
}