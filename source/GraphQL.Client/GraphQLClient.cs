using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Client.Models;
using GraphQL.Client.Requests;
using GraphQL.Client.Schema;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client {
    public class GraphQLClient {

        private readonly IGraphQLRequestExecutor RequestExecutor;
        private readonly IRequestBuilder RequestBuilder;
        private readonly IResultBuilder ResultBuilder;
        private readonly IPartialSchemaProvider PartialSchemaProvider;
        private readonly ISchemaValidator SchemaValidator;

        public GraphQLClient(
            IGraphQLRequestExecutor requestExecutor,
            IRequestBuilder requestBuilder,
            IResultBuilder resultBuilder,
            IPartialSchemaProvider partialSchemaProvider,
            ISchemaValidator schemaValidator
        ) {
            RequestExecutor = requestExecutor;
            RequestBuilder = requestBuilder;
            PartialSchemaProvider = partialSchemaProvider;
            SchemaValidator = schemaValidator;
            ResultBuilder = resultBuilder;
        }

        /// <summary>
        /// Are the mutations and queries supported by this client a subset of
        /// those in the input schema, and do the models in this assembly match
        /// the types specified by the schena.
        /// </summary>
        public Result ValidateAgainstSchema(string schemaString) {
            var testSchema = GraphQL.Types.Schema.For(schemaString);

            return SchemaValidator.ValidateAll(
                PartialSchemaProvider.Queries.Values.Concat(PartialSchemaProvider.Mutations.Values),
                testSchema);
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult>(string name, string operationName = null) {
            return await ExecuteRequest<TResult, Object, Object, Object>(name, null, null, null, operationName);
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult, TArg1>(string name, TArg1 arg1, string operationName = null) {
            return await ExecuteRequest<TResult, TArg1, Object, Object>(name, arg1, null, null, operationName);
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2, string operationName = null) {
            return await ExecuteRequest<TResult, TArg1, TArg2, Object>(name, arg1, arg2, null, operationName);
        }
        public async Task<Result<TResult>> ExecuteRequest<TResult, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, string operationName = null) {
            string operationType;
            FieldType fieldType;

            if (string.IsNullOrWhiteSpace(name)) {
                return Results.Fail<TResult>(new FluentResults.ExceptionalError(new ArgumentNullException(nameof(name))));
            }

            operationName = string.IsNullOrWhiteSpace(operationName) ? name : operationName;

            if (PartialSchemaProvider.Queries.TryGetValue(name, out fieldType)) {
                operationType = "query";
            } else if (PartialSchemaProvider.Mutations.TryGetValue(name, out fieldType)) {
                operationType = "mutation";
            } else {
                return Results.Fail<TResult>(new FluentResults.ExceptionalError(new KeyNotFoundException($"No query or mutation \"{name}\" was found.")));
            }

            var request = RequestBuilder.BuildRequest<TResult, TArg1, TArg2, TArg3>(name, operationName, operationType, fieldType, arg1, arg2, arg3);

            if (request.IsFailed) {
                return request.ToResult<TResult>();
            }

            var result = await ExecuteRequest(request.Value);

            if (result.Errors.Count == 0) {
                return ResultBuilder.BuildTResult<TResult>(result, request.Value, name, fieldType.ResolvedType.IsNonNullGraphType());
            }

            return Results.Fail<TResult>(result.Errors.ToString());
        }

        public async Task<GraphQLResult> ExecuteRequest(
                string operationType,
                string operationName,
                VariableDefinitions variableDefinitions,
                ISelection selection,
                JObject variables) =>
            await ExecuteRequest(RequestBuilder.AstToRequest(operationType, operationName, variableDefinitions, selection, variables));

        public async Task<GraphQLResult> ExecuteRequest(GraphQLRequest request) {
            return await RequestExecutor.ExecuteRequest(request);
        }

    }
}