using FluentResults;
using GraphQL.Client.Models;
using GraphQL.Language.AST;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client.Requests {

    public interface IRequestBuilder {
        Result<GraphQLRequest> BuildRequest<TResult, TArg1, TArg2, TArg3>(
            string name,
            string operationName,
            string operationType,
            FieldType fieldType,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3
        );

        GraphQLRequest AstToRequest(
            string operationType,
            string operationName,
            VariableDefinitions variableDefinitions,
            ISelection selection,
            JObject variables
        );
    }

}