using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Models;
using GraphQL.Language.AST;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client {

    public interface IGraphQLClient {

        Task<Result<TResult>> ExecuteRequest<TResult>(string name, string operationName = null);
        Task<Result<TResult>> ExecuteRequest<TResult, TArg1>(string name, TArg1 arg1, string operationName = null);
        Task<Result<TResult>> ExecuteRequest<TResult, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2, string operationName = null);
        Task<Result<TResult>> ExecuteRequest<TResult, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, string operationName = null);

        Task<GraphQLResult> ExecuteRequest(
                string operationType,
                string operationName,
                VariableDefinitions variableDefinitions,
                ISelection selection,
                JObject variables);

        Task<GraphQLResult> ExecuteRequest(GraphQLRequest request);       

        Result ValidateAgainstSchema(string schemaString); 
    }

}