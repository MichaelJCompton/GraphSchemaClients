using System.Threading.Tasks;
using GraphQL.Client.Models;

namespace GraphQL.Client.Requests {
    public interface IGraphQLRequestExecutor {
        Task<GraphQLResult> ExecuteRequest(GraphQLRequest request);
    }
}