using System.Threading.Tasks;
using FluentAssertions;
using GraphQL.Client.Models;
using GraphQL.Client.Requests;
using GraphQL.Client.tests.TestTypes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GraphQL.Client.tests.Requests {
    public class ResultBuilderFixture {

        [Test]
        public void ValidResultDeserializes() {
            var resultBuilderUnderTest = new ResultBuilder();

            var result = resultBuilderUnderTest.BuildTResult<TestResult>(TestValues.SerializedTestResultAsGraphQLResult, null, TestValues.QueryName, true);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(TestValues.TestResult);
        }

        [Test]
        public void NullWhenNonNullRequiredAreErrors() {
            var gqlResult = $"{{\"Data\":{{\"{TestValues.QueryName}\":null}},\"Errors\":[]}}";
            var request = new GraphQLRequest { Query = "a query" };
            var resultBuilderUnderTest = new ResultBuilder();

            var result = resultBuilderUnderTest.BuildTResult<TestResult>(JsonConvert.DeserializeObject<GraphQLResult>(gqlResult), request, TestValues.QueryName, true);

            result.IsSuccess.Should().BeFalse();
            var error = (result.Errors[0] as GraphQLErrorReason).GraphQLError;
            error.Message.Should().Be("Returned null, but GraphQL required non-null.");
            error.Extensions.Request.Should().Be(request);
            error.Extensions.Response.Should().Be(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<GraphQLResult>(gqlResult), Formatting.Indented));
        }

        [Test]
        public void NullOKWhenSchemaAllowsIt() {
            var gqlResult = $"{{\"Data\":{{\"{TestValues.QueryName}\":null}},\"Errors\":[]}}";
            var request = new GraphQLRequest { Query = "a query" };
            var resultBuilderUnderTest = new ResultBuilder();

            var result = resultBuilderUnderTest.BuildTResult<TestResult>(JsonConvert.DeserializeObject<GraphQLResult>(gqlResult), request, TestValues.QueryName, false);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }

        [Test]
        public void WrongResultStructureIsJsonError() {
            var gqlResult = $"{{\"Data\":{{\"{TestValues.QueryName}\":{{\"notValid\": \"yep not a TestResult\"}}}},\"Errors\":[]}}";
            var request = new GraphQLRequest { Query = "a query" };
            var resultBuilderUnderTest = new ResultBuilder();

            var result = resultBuilderUnderTest.BuildTResult<TestResult>(JsonConvert.DeserializeObject<GraphQLResult>(gqlResult), request, TestValues.QueryName, false);

            result.IsSuccess.Should().BeFalse();
            var error = (result.Errors[0] as GraphQLErrorReason).GraphQLError;
            error.Message.Should().Be("Json Serialization Exception while reading GraphQL response");
            error.Extensions.Request.Should().NotBeNull();
            error.Extensions.Response.Should().Be(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<GraphQLResult>(gqlResult), Formatting.Indented));
        }

    }
}