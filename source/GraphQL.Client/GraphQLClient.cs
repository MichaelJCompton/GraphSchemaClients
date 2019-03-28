using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Client.Models;
using GraphQL.Client.Requests;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client {
    public class GraphQLClient {

        private readonly HttpClient Client;

        private Dictionary<string, FieldType> Queries = new Dictionary<string, FieldType>();
        private Dictionary<string, FieldType> Mutations = new Dictionary<string, FieldType>();

        public GraphQLClient(HttpClient client) {
            Client = client;
        }

        // FIXME: add for testing : iscompatiblewith(schema-string)
        // tests all the types and queries and mutations against the schema

        public void WithSchema(string schemaString) {
            var graphQLSchema = GraphQL.Types.Schema.For(schemaString);

            foreach (var field in graphQLSchema.Query.Fields) {
                Queries[field.Name] = field;
            }
            foreach (var field in graphQLSchema.Mutation.Fields) {
                Mutations[field.Name] = field;
            }
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

            if (Queries.TryGetValue(name, out fieldType)) {
                operationType = "query";
            } else if (Mutations.TryGetValue(name, out fieldType)) {
                operationType = "mutation";
            } else {
                return Results.Fail<TResult>(new FluentResults.ExceptionalError(new KeyNotFoundException($"No query or mutation \"{name}\" was found.")));
            }

            var request = RequestBuilder.BuildRequest<TResult, TArg1, TArg2, TArg3>(name, operationName, operationType, fieldType, arg1, arg2, arg3);

            if(request.IsFailed) {
                return request.ToResult<TResult>();
            }

            var result = await ExecuteRequest(request.Value);

            if (result.Errors.Count == 0) {
                TResult asTResult = result.Data[name].ToObject<TResult>();
                if (fieldType.ResolvedType.IsNonNullGraphType() && asTResult == null) {
                    return Results.Fail<TResult>($"Returned null, but GraphQL required {fieldType.ResolvedType.GetNamedType().Name} to be non-null.");
                }
                return Results.Ok(asTResult);
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

        // Built around ideas from https://johnthiriet.com/efficient-post-calls/
        // not yet tested properly
        public async Task<GraphQLResult> ExecuteRequest(GraphQLRequest request) {

            using(var httpContent = CreateHttpContent(request)) {

                // Base address should be set on the client from configuration.
                using(var response = await Client.PostAsync("", httpContent)) // FIXME: need cancelation tokens????
                {
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<GraphQLResult>(responseContent);
                }
            }

            // FIXME: what to do with errors in here??? maybe should add them to
            // the errors payload of the result and return that ... that'll mean
            // a client has to deal with errors from this and from the backend
            // this is calling 
            //
            // try { } catch() { } and return error if we get to the end.
            //
            // probably can also use something like polly
            // https://github.com/App-vNext/Polly to retry on transient errors??
        }

        private HttpContent CreateHttpContent(GraphQLRequest query) {
            HttpContent httpContent;

            var ms = new MemoryStream();
            SerializeJsonIntoStream(query, ms);
            ms.Seek(0, SeekOrigin.Begin);
            httpContent = new StreamContent(ms, 1024);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return httpContent;
        }

        private void SerializeJsonIntoStream(object value, Stream stream) {
            using(var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true)) {
                using(var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None }) {
                    var js = new JsonSerializer();
                    js.Serialize(jtw, value);
                    jtw.Flush();
                }
            }
        }

    }
}