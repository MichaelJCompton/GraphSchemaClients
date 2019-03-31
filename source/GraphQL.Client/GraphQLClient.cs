using System;
using System.Collections.Generic;
using System.IO;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client {
    public class GraphQLClient {

        private readonly HttpClient Client;

        private readonly Dictionary<string, FieldType> Queries = new Dictionary<string, FieldType>();
        private readonly Dictionary<string, FieldType> Mutations = new Dictionary<string, FieldType>();

        public GraphQLClient(HttpClient client) {
            Client = client;
        }

        // FIXME: add for testing : iscompatiblewith(schema-string)
        // tests all the types and queries and mutations against the schema

        public void WithSchema(string schemaString) {
            var graphQLSchema = GraphQL.Types.Schema.For(schemaString);

            if (graphQLSchema.Query != null) {
                foreach (var field in graphQLSchema.Query.Fields) {
                    Queries[field.Name] = field;
                }
            }
            if (graphQLSchema.Mutation != null) {
                foreach (var field in graphQLSchema.Mutation.Fields) {
                    Mutations[field.Name] = field;
                }
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

            if (request.IsFailed) {
                return request.ToResult<TResult>();
            }

            var result = await ExecuteRequest(request.Value);

            if (result.Errors.Count == 0) {
                return BuildTResult<TResult>(result, request.Value, name, fieldType.ResolvedType.IsNonNullGraphType());
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
            // FIXME: need cancelation tokens????
            //
            // probably can also use something like polly
            // https://github.com/App-vNext/Polly to retry on transient errors??

            try {
                using(var httpContent = CreateHttpContent(request)) {
                    // Base address should be set on the http client.
                    using(var response = await Client.PostAsync("", httpContent)) {
                        using(Stream responseStream = await response.Content.ReadAsStreamAsync()) {
                            if (response.IsSuccessStatusCode) {
                                try {
                                    return DeserializeJsonFromStream<GraphQLResult>(responseStream);
                                } catch (JsonSerializationException ex) {
                                    // try rereading the stream ... is this always safe?
                                    return BuildGraphQLErrorResult(
                                        "Json Serialization Exception while reading GraphQL response",
                                        ex, request, await response.Content.ReadAsStringAsync());
                                }
                            } else {
                                return BuildGraphQLErrorResult(
                                    $"HTTP response is not success (code {response.StatusCode})",
                                    null, request, await response.Content.ReadAsStringAsync());
                            }
                        }
                    }
                }
            } catch (Exception ex) { // Just catch WebException ??
                return BuildGraphQLErrorResult(
                    "Exception while processing request",
                    ex, request, null);
            }
        }

        // see 
        // https://www.newtonsoft.com/json/help/html/Performance.htm
        // and
        // https://johnthiriet.com/efficient-api-calls/
        private T DeserializeJsonFromStream<T>(Stream stream) {
            if (stream == null || stream.CanRead == false)
                return default(T);

            using(StreamReader responseStreamReader = new StreamReader(stream)) {
                using(JsonReader jsonReader = new JsonTextReader(responseStreamReader)) {
                    JsonSerializer serializer = new JsonSerializer();

                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }

        private GraphQLResult BuildGraphQLErrorResult(string message, Exception exception, GraphQLRequest graphQLRequest, string responseString) {
            var result = new GraphQLResult();
            result.Errors = new JArray();
            result.Errors.Add(JObject.FromObject(BuildGraphQLError(message, exception, graphQLRequest, responseString)));
            return result;
        }

        private GraphQLError BuildGraphQLError(string message, Exception exception, GraphQLRequest graphQLRequest, string responseString) {
            /* fixformat ignore:start */
            return new GraphQLError {
                Message = message,
                Extensions = new GraphQLErrorExtension {
                    Exception = exception,
                    Response = responseString,
                    Request = graphQLRequest
                }
            };
            /* fixformat ignore:end */
        }

        private Result<TResult> BuildTResult<TResult>(GraphQLResult result, GraphQLRequest request, string requestName, bool nonNull) {
            try {
                var data = result.Data[requestName];
                TResult asTResult;
                if (data == null) {
                    asTResult = default(TResult);
                } else {
                    // Not sure yet if this is 100% right.  Maybe the serializer
                    // should be an option, cause there might be cases where you
                    // want to ingore the extra properties.
                    asTResult = data.ToObject<TResult>(new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error });
                }

                if (nonNull && (data == null || asTResult == null)) {
                    // Not sure if this would occur.  A GraphQL
                    // server should have already errored and be returning that
                    // error, right?

                    return Results.Fail<TResult>(
                        new GraphQLErrorReason(
                            BuildGraphQLError("Returned null, but GraphQL required non-null.",
                                null, request, JsonConvert.SerializeObject(result, Formatting.Indented))));
                }
                return Results.Ok(asTResult);
            } catch (JsonSerializationException ex) {
                return Results.Fail<TResult>(new GraphQLErrorReason(
                    BuildGraphQLError("Json Serialization Exception while reading GraphQL response",
                        ex, request, JsonConvert.SerializeObject(result, Formatting.Indented))));
            }
        }

        // Built around ideas from https://johnthiriet.com/efficient-post-calls/
        private HttpContent CreateHttpContent(GraphQLRequest request) {
            HttpContent httpContent;

            var ms = new MemoryStream();
            SerializeJsonIntoStream(request, ms);
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