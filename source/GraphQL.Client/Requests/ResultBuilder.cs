using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client.Requests {
    public class ResultBuilder : IResultBuilder {

        public ResultBuilder() { }

        public Result<TResult> BuildTResult<TResult>(GraphQLResult result, GraphQLRequest request, string requestName, bool nonNull) {
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

        public GraphQLResult BuildGraphQLErrorResult(string message, Exception exception, GraphQLRequest graphQLRequest, string responseString) {
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

    }
}