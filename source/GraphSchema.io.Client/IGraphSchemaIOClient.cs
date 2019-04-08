using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client;
using GraphSchema.io.Client.Models;

namespace GraphSchema.io.Client {
    public interface IGraphSchemaIOClient : IGraphQLClient {
        Task<Result<GraphSchema.io.Client.Models.Environment>> GetEnvironment(string id);
        Task<Result<List<GraphSchema.io.Client.Models.Environment>>> QueryEnvironment(string name);
        Task<Result<DgraphInstance>> GetDgraphInstance(string dgraphId);
        Task<Result<DgraphInstance>> QueryDgraphInstance(string environmentId);
        Task<Result<DgraphInstance>> AddDgraphInstance(DgraphInstanceInput instance);
        Task<Result<DgraphInstance>> AddDgraphInstanceAndWait(DgraphInstanceInput instance);
        Task<Result<string>> DeleteDgraphInstance(string dgraphId);
    }
}