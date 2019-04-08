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
using GraphQL.Client.tests.TestTypes;
using GraphQL.Types;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GraphQL.Client.tests.Requests {
    public class HttpGraphQLRequestExecutorFixture {

        private FieldType TestField;
        private string TestName = "aTestQuery";
        private string Schema = @"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
}
";

        private string Arg1 = "ID-123";
        private string Arg2 = "a string arg";
        private TestType Arg3 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };

        [OneTimeSetUp]
        public void CreateTestField() {
            var graphQLSchema = GraphQL.Types.Schema.For(Schema);

            TestField = graphQLSchema.Query.Fields.FirstOrDefault();
        }

        [Test]
        public async Task ValidGraphQLResultISReturnedUnchanged() {
            var stringContent = "{\"Data\":{\"aVal\":27},\"Errors\":[{\"message\":\"a message\"}]}";
            var executorUnderTest = BuildTestRequestExecutor(stringContent);
            var request = BuildGraphQLRequest();

            var result = await executorUnderTest.ExecuteRequest(new GraphQLRequest());

            JsonConvert.SerializeObject(result).Should().Be(stringContent);
        }

        [Test]
        public async Task ExceptionIsErrorResult() {
            var executorUnderTest = BuildTestRequestExecutor();
            var request = BuildGraphQLRequest();

            var result = await executorUnderTest.ExecuteRequest(request);

            result.Data.Should().BeNull();
            var error = result.Errors[0].ToObject<GraphQLError>();
            error.Message.Should().Be("Exception while processing request");
            error.Extensions.Exception.Should().NotBeNull();
            error.Extensions.Request.Should().BeEquivalentTo(request);
            error.Extensions.Response.Should().BeNull(because: "the error was thrown before a response was recieved");
        }

        [Test]
        public async Task InvalidJsonIsErrorResult() {
            var stringContent = "{\"Data\":{\"aVal\":27},\"Errors\":[{\"message\":\"a message, but this is invalid JSON\"";
            var executorUnderTest = BuildTestRequestExecutor(stringContent);
            var request = BuildGraphQLRequest();

            var result = await executorUnderTest.ExecuteRequest(request);

            result.Data.Should().BeNull();
            var error = result.Errors[0].ToObject<GraphQLError>();
            error.Message.Should().Be("Json Serialization Exception while reading GraphQL response");
            error.Extensions.Exception.Should().NotBeNull();
            error.Extensions.Request.Should().BeEquivalentTo(request);
            error.Extensions.Response.Should().Be(stringContent);
        }

        [Test]
        public async Task HTTPNotOKIsErrorResult() {
            var stringContent = "An Error";
            // Not sure how often this should occur.  Most GraphQL servers
            // should return an OK with an error payload, but there'll also be
            // http error cases.
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            response.Content = new StringContent(stringContent);
            var executorUnderTest = BuildTestRequestExecutor(responseMessage: response);
            var request = BuildGraphQLRequest();

            var result = await executorUnderTest.ExecuteRequest(request);

            result.Data.Should().BeNull();
            var error = result.Errors[0].ToObject<GraphQLError>();
            error.Message.Should().Be($"HTTP response is not success (code {System.Net.HttpStatusCode.Unauthorized})");
            error.Extensions.Exception.Should().BeNull();
            error.Extensions.Request.Should().BeEquivalentTo(request);
            error.Extensions.Response.Should().Be(stringContent);
        }

        private HttpGraphQLRequestExecutor BuildTestRequestExecutor(string responseContent = null, HttpResponseMessage responseMessage = null) {
            SubstituteHttpMessageHandler messageHandler;
            if (responseMessage != null) {
                messageHandler = new SubstituteHttpMessageHandler(responseMessage);
            } else if (!string.IsNullOrWhiteSpace(responseContent)) {
                messageHandler = new SubstituteHttpMessageHandler(responseContent);
            } else {
                messageHandler = new SubstituteHttpMessageHandler();
            }

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new System.Uri("http://fake.fake");

            var requestExecutor = new HttpGraphQLRequestExecutor(httpClient, new ResultBuilder()); 

            return requestExecutor;
        }

        private GraphQLRequest BuildGraphQLRequest() {
            return (new RequestBuilder())
                .BuildRequest<TestResult, string, string, TestType>(
                    TestName, TestName, "query", TestField, Arg1, Arg2, Arg3).Value;
        }

        public class SubstituteHttpMessageHandler : HttpMessageHandler {

            private readonly string ResponseContent;

            private readonly HttpResponseMessage Response;

            public SubstituteHttpMessageHandler() { }

            public SubstituteHttpMessageHandler(string responseContent) {
                ResponseContent = responseContent;
            }

            public SubstituteHttpMessageHandler(HttpResponseMessage response) {
                Response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                if (Response != null) {
                    return Task.FromResult(Response);
                } else if (!string.IsNullOrWhiteSpace(ResponseContent)) {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    response.Content = new StringContent(ResponseContent);
                    return Task.FromResult(response);
                } else {
                    throw new WebException("An Error");
                }
            }
        }
    }
}