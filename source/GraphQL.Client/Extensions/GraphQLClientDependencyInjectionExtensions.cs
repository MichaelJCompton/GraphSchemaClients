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

            services.AddBaseGraphQLClientServices(schema, modelNamespaces);
            services.AddHttpClient<IGraphQLRequestExecutor, HttpGraphQLRequestExecutor>(configureHttpClient);
            services.AddTransient<TClientInterface, TClient>();

            return services;
        }

        public static IServiceCollection AddGraphQLClient<TClientInterface, TClient>(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient> configureHttpClient,
            string schema,
            IEnumerable<string> modelNamespaces = null)
        where TClientInterface : class, IGraphQLClient where TClient : class, TClientInterface {

            services.AddBaseGraphQLClientServices(schema, modelNamespaces);
            services.AddHttpClient<IGraphQLRequestExecutor, HttpGraphQLRequestExecutor>(configureHttpClient);
            services.AddTransient<TClientInterface, TClient>();

            return services;
        }

        private static IServiceCollection AddBaseGraphQLClientServices(
            this IServiceCollection services,
            string schema,
            IEnumerable<string> modelNamespaces = null) {

            services.AddTransient<IFieldValidator, FieldValidator>();
            services.AddTransient<ISchemaValidator, SchemaValidator>();

            var typeValidator = new TypeValidator(modelNamespaces ?? new List<string>());
            services.AddSingleton<ITypeValidator>(typeValidator);

            services.AddTransient<IRequestBuilder, RequestBuilder>();
            services.AddTransient<IResultBuilder, ResultBuilder>();

            var schemaPrivider = new PartialSchemaProvider();
            schemaPrivider.WithSchema(schema);
            services.AddSingleton<IPartialSchemaProvider>(schemaPrivider);

            return services;
        }

    }
}