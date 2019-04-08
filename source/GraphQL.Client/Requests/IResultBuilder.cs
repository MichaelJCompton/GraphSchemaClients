using System;
using FluentResults;
using GraphQL.Client.Models;

namespace GraphQL.Client.Requests {
    public interface IResultBuilder {
        Result<TResult> BuildTResult<TResult>(GraphQLResult result, GraphQLRequest request, string requestName, bool nonNull);
        GraphQLResult BuildGraphQLErrorResult(string message, Exception exception, GraphQLRequest graphQLRequest, string responseString);
    }
}