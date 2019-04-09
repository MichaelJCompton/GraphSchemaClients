using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client;
using GraphQL.Client.Requests;
using GraphQL.Client.Schema;
using GraphSchema.io.Client.Models;
using GraphSchema.io.Client.Resources;

namespace GraphSchema.io.Client {
    public class GraphSchemaIOClient : GraphQLClient, IGraphSchemaIOClient {
        public GraphSchemaIOClient(
            IGraphQLRequestExecutor requestExecutor,
            IRequestBuilder requestBuilder,
            IResultBuilder resultBuilder,
            IPartialSchemaProvider partialSchemaProvider,
            ISchemaValidator schemaValidator) : base(requestExecutor, requestBuilder, resultBuilder, partialSchemaProvider, schemaValidator) {
        }

        public async Task<Result<GraphSchema.io.Client.Models.Environment>> GetEnvironment(string id) {
            return await ExecuteRequest<GraphSchema.io.Client.Models.Environment, string>("getEnvironment", id);
        }

        public async Task<Result<List<GraphSchema.io.Client.Models.Environment>>> QueryEnvironment(string name) {
            return await ExecuteRequest<List<GraphSchema.io.Client.Models.Environment>, List<string>>("queryEnvironment", new List<string> { name } );
        }

        public async Task<Result<DgraphInstance>> GetDgraphInstance(string dgraphId) {
            return await ExecuteRequest<DgraphInstance, string>("getDgraphInstance", dgraphId);
        }

        public async Task<Result<DgraphInstance>> QueryDgraphInstance(string environmentId) {
            return await ExecuteRequest<DgraphInstance, string>("queryDgraphInstance", environmentId);
        }

        public async Task<Result<DgraphInstance>> AddDgraphInstance(DgraphInstanceInput instance) {
            return await ExecuteRequest<DgraphInstance, DgraphInstanceInput>("addDgraphInstance", instance);
        }

        public async Task<Result<DgraphInstance>> AddDgraphInstanceAndWait(DgraphInstanceInput instance) {
            var addResult = await AddDgraphInstance(instance);

            if (addResult.IsFailed) {
                return addResult;
            }

            // FIXME: need backoff policy and cancelation in here

            for (int i = 1; i < 4; i++) {
                Thread.Sleep(TimeSpan.FromSeconds(30 * i));
                var getResult = await GetDgraphInstance(addResult.Value.Id);
                if (getResult.IsSuccess && getResult.Value != null) {
                    return getResult;
                }
            }

            var fail = Results.Fail<DgraphInstance>("Instance doesn't seem to be up yet");
            fail.WithSuccess(new Success("Successesfully added instance").WithMetadata("Id", addResult.Value.Id));
            return fail;
        }

        public async Task<Result<string>> DeleteDgraphInstance(string dgraphId) {
            return await ExecuteRequest<string, string>("deleteDgraphInstance", dgraphId);
        }
    }
}