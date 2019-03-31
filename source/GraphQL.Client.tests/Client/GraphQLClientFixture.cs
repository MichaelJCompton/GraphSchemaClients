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

namespace GraphQL.Client.tests.Client {

    public class GraphQLClientFixture {

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

        #region Basic Execution

        [Test]
        public async Task ValidGraphQLResultISReturnedUnchanged() {
            var stringContent = "{\"Data\":{\"aVal\":27},\"Errors\":[{\"message\":\"a message\"}]}";
            var clientUnderTest = BuildTestGraphQLClient(stringContent);
            var request = BuildGraphQLRequest();

            var result = await clientUnderTest.ExecuteRequest(new GraphQLRequest());

            JsonConvert.SerializeObject(result).Should().Be(stringContent);
        }

        [Test]
        public async Task ExceptionIsErrorResult() {
            var clientUnderTest = BuildTestGraphQLClient();
            var request = BuildGraphQLRequest();

            var result = await clientUnderTest.ExecuteRequest(request);

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
            var clientUnderTest = BuildTestGraphQLClient(stringContent);
            var request = BuildGraphQLRequest();

            var result = await clientUnderTest.ExecuteRequest(request);

            result.Data.Should().BeNull();
            var error = result.Errors[0].ToObject<GraphQLError>();
            error.Message.Should().Be("Json Serialization Exception while reading GraphQL response");
            error.Extensions.Exception.Should().NotBeNull();
            error.Extensions.Request.Should().BeEquivalentTo(request);
            error.Extensions.Response.Should().Be(stringContent);
        }

        #endregion

        #region typed execution

        [Test]
        public async Task ValidRequestDeserializes() {
            var testResult = new TestResult {
                AnInt = 24,
                Edge = Arg3
            };
            var serializedResult = JsonConvert.SerializeObject(testResult);
            var stringContent = $"{{\"Data\":{{\"{TestName}\":{serializedResult}}},\"Errors\":[]}}";

            var clientUnderTest = BuildTestGraphQLClient(stringContent);

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestName, Arg1, Arg2, Arg3);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(testResult);
        }

        [Test]
        public async Task NullWhenNonNullRequiredAreErrors() {

            var stringContent = $"{{\"Data\":{{\"{TestName}\":null}},\"Errors\":[]}}";

            var clientUnderTest = BuildTestGraphQLClient(stringContent);

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestName, Arg1, Arg2, Arg3);
            result.IsSuccess.Should().BeFalse();
            var error = (result.Errors[0] as GraphQLErrorReason).GraphQLError;
            error.Message.Should().Be("Returned null, but GraphQL required non-null.");
            error.Extensions.Request.Should().NotBeNull();
            error.Extensions.Response.Should().Be(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<GraphQLResult>(stringContent), Formatting.Indented));
        }

        [Test]
        public async Task NullOKWhenSchemaAllowsIt() {
            var stringContent = $"{{\"Data\":{{\"{TestName}\":null}},\"Errors\":[]}}";
            var messageHandler = new SubstituteHttpMessageHandler(stringContent);
            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new System.Uri("http://fake.fake");

            var clientUnderTest = new GraphQLClient(httpClient);
            clientUnderTest.WithSchema("type Query { aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult }");

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestName, Arg1, Arg2, Arg3);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }

        [Test]
        public async Task NameMustBeNonNull() {
            var clientUnderTest = new GraphQLClient(new HttpClient());

            var result = await clientUnderTest.ExecuteRequest<Object>(null);

            result.IsFailed.Should().BeTrue();
            (result.Errors[0] as ExceptionalError).Exception.Should().BeOfType<ArgumentNullException>();
        }

        [Test]
        public async Task NameMustBePresent() {
            var clientUnderTest = new GraphQLClient(new HttpClient());

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>("aDifferetName", null, null, null);

            result.IsFailed.Should().BeTrue();
            (result.Errors[0] as ExceptionalError).Exception.Should().BeOfType<KeyNotFoundException>();
        }

        [Test]
        public async Task WrongResultStructureIsJsonError() {

            var stringContent = $"{{\"Data\":{{\"{TestName}\":{{\"notValid\": \"yep not a TestResult\"}}}},\"Errors\":[]}}";

            var clientUnderTest = BuildTestGraphQLClient(stringContent);

            var result = await clientUnderTest.ExecuteRequest<TestResult, string, string, TestType>(TestName, Arg1, Arg2, Arg3);
            result.IsSuccess.Should().BeFalse();
            var error = (result.Errors[0] as GraphQLErrorReason).GraphQLError;
            error.Message.Should().Be("Json Serialization Exception while reading GraphQL response");
            error.Extensions.Request.Should().NotBeNull();
            error.Extensions.Response.Should().Be(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<GraphQLResult>(stringContent), Formatting.Indented));
        }

        #endregion

        private GraphQLClient BuildTestGraphQLClient(string responseContent = null, HttpResponseMessage responseMessage = null) {
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

            var client = new GraphQLClient(httpClient);
            client.WithSchema(Schema);
            return client;
        }

        private GraphQLRequest BuildGraphQLRequest() {
            return RequestBuilder.BuildRequest<TestResult, string, string, TestType>(TestName, TestName, "query", TestField, Arg1, Arg2, Arg3).Value;
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