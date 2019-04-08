using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentResults;
using GraphQL.Client.Models;
using GraphQL.Client.Requests;
using GraphQL.Client.Schema;
using GraphQL.Client.tests.TestTypes;
using GraphQL.Types;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace GraphQL.Client.tests.Client {

    public class GraphQLClientFixture {

        private IGraphQLRequestExecutor ReqExecutor;
        private IRequestBuilder ReqBuilder;
        private IResultBuilder ResBuilder;
        private IPartialSchemaProvider SchemaProv;
        private ISchemaValidator Validator;

        private FieldType TestQuery;
        private FieldType TestMutation;

        public string Schema = @"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
}

type Mutation {
    aTestMutation(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
}
";

        [OneTimeSetUp]
        public void CreateTestField() {
            var graphQLSchema = GraphQL.Types.Schema.For(Schema);

            TestQuery = graphQLSchema.Query.Fields.FirstOrDefault();
            TestMutation = graphQLSchema.Mutation.Fields.FirstOrDefault();
        }

        [OneTimeSetUp]
        public void CreateSubstitutes() {
            ReqExecutor = Substitute.For<IGraphQLRequestExecutor>();
            ReqBuilder = Substitute.For<IRequestBuilder>();
            ResBuilder = Substitute.For<IResultBuilder>();
            SchemaProv = Substitute.For<IPartialSchemaProvider>();
            Validator = Substitute.For<ISchemaValidator>();
        }

        [Test]
        public async Task UnTypedGraphQLRequestPassesStraightThrough() {
            var json = "{\"Data\":{\"aVal\":27},\"Errors\":[{\"message\":\"a message\"}]}";
            var clientUnderTest = BuildTestGraphQLClient(resultJson: json);
            var request = new GraphQLRequest { Query = "A Query", OperationName = "An op" };

            var result = await clientUnderTest.ExecuteRequest(request);

            JsonConvert.SerializeObject(result).Should().Be(json);
            await ReqExecutor.Received().ExecuteRequest(request);
        }

        [Test]
        public async Task NameMustBeNonNull() {
            var clientUnderTest = BuildTestGraphQLClient();

            var result = await clientUnderTest.ExecuteRequest<Object>(null);

            result.IsFailed.Should().BeTrue();
            (result.Errors[0] as ExceptionalError).Exception.Should().BeOfType<ArgumentNullException>();
        }

        [Test]
        public async Task NameMustBePresent() {
            var clientUnderTest = BuildTestGraphQLClient();

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>("aDifferetName", null, null, null);

            result.IsFailed.Should().BeTrue();
            (result.Errors[0] as ExceptionalError).Exception.Should().BeOfType<KeyNotFoundException>();
        }

        [Test]
        public async Task RequestBuilderGetsQuery() {
            var clientUnderTest = BuildTestGraphQLClient();

            ReqBuilder.BuildRequest<TestResult, string, string, TestType>(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<FieldType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TestType>())
                .Returns(Results.Fail<GraphQLRequest>("Didn't try"));

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestValues.QueryName, TestValues.Arg1, TestValues.Arg2, TestValues.Arg3);

            ReqBuilder.Received().BuildRequest<TestResult, string, string, TestType>(TestValues.QueryName, TestValues.QueryName, "query", TestQuery, TestValues.Arg1, TestValues.Arg2, TestValues.Arg3);
        }

        [Test]
        public async Task RequestBuilderGetsMutation() {
            var clientUnderTest = BuildTestGraphQLClient();

            ReqBuilder.BuildRequest<TestResult, string, string, TestType>(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<FieldType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TestType>())
                .Returns(Results.Fail<GraphQLRequest>("Didn't try"));

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestValues.MutationName, TestValues.Arg1, TestValues.Arg2, TestValues.Arg3);

            ReqBuilder.Received().BuildRequest<TestResult, string, string, TestType>(TestValues.MutationName, TestValues.MutationName, "mutation", TestMutation, TestValues.Arg1, TestValues.Arg2, TestValues.Arg3);

        }

        private GraphQLClient BuildTestGraphQLClient(string resultJson = null) {

            if (resultJson != null) {
                ReqExecutor.ExecuteRequest(Arg.Any<GraphQLRequest>()).Returns(JsonConvert.DeserializeObject<GraphQLResult>(resultJson));
            }

            SchemaProv.Queries.Returns(new Dictionary<string, FieldType>() { { TestValues.QueryName, TestQuery } });
            SchemaProv.Mutations.Returns(new Dictionary<string, FieldType>() { { TestValues.MutationName, TestMutation } });

            var client = new GraphQLClient(ReqExecutor, ReqBuilder, ResBuilder, SchemaProv, Validator);

            return client;
        }

    }
}