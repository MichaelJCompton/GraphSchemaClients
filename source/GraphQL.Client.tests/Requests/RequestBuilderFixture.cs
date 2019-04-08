using System;
using System.Collections.Generic;
using System.Linq;
using Assent;
using Assent.Namers;
using FluentAssertions;
using GraphQL.Client.Attributes;
using GraphQL.Client.Models;
using GraphQL.Client.Requests;
using GraphQL.Client.tests.TestTypes;
using GraphQL.Types;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GraphQL.Client.tests.Requests
{
    public class RequestBuilderFixture
    {

        private FieldType TestField;
        private string TestName = "aTestQuery";

        private Assent.Configuration RequestBuilderAssentConfiguration;

        [OneTimeSetUp]
        public void SetupAssent() {
            RequestBuilderAssentConfiguration = new Assent.Configuration()
                .UsingNamer(new SubdirectoryNamer("RequestBuilderApproved"));
        }

        [OneTimeSetUp]
        public void CreateTestField() {
            var graphQLSchema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
}
");

            TestField = graphQLSchema.Query.Fields.FirstOrDefault();
        }

        [Test]
        public void RequestIsAsExpected() {
            var arg1 = "ID-123";
            var arg2 = "a string arg";
            var arg3 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, string, string, TestType>(TestName, TestName, "query", TestField, arg1, arg2, arg3);

            result.IsSuccess.Should().BeTrue();
            this.Assent(JsonConvert.SerializeObject(result.Value, Formatting.Indented), RequestBuilderAssentConfiguration);
        }

        [Test]
        public void RequestIsAsExpectedWithListTypes() {
            var graphQLSchema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: [TestType!]!): [TestResult!]!
}
");
            var testField = graphQLSchema.Query.Fields.FirstOrDefault();
            var arg1 = "ID-123";
            var arg2 = "a string arg";
            var arg3 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<List<TestResult>, string, string, List<TestType>>(TestName, TestName, "query", testField, arg1, arg2, new List<TestType>{ arg3 });

            result.IsSuccess.Should().BeTrue();
            this.Assent(JsonConvert.SerializeObject(result.Value, Formatting.Indented), RequestBuilderAssentConfiguration);
        }

        [Test]
        public void RequestIsAsExpectedWithNullArgs() {
            var arg1 = "ID-123";
            string arg2 = null;  // schema marks this as String (not String!), so null is ok
            var arg3 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, string, string, TestType>(TestName, TestName, "query", TestField, arg1, arg2, arg3);

            result.IsSuccess.Should().BeTrue();
            this.Assent(JsonConvert.SerializeObject(result.Value, Formatting.Indented), RequestBuilderAssentConfiguration);
        }

        [Test]
        public void RequestIsAsExpectedWithLessArgs() {
            var graphQLSchema = GraphQL.Types.Schema.For("type Query { aTestQuery(arg1: ID!, arg2: TestType!): TestResult! }");
            var testField = graphQLSchema.Query.Fields.FirstOrDefault();
            var arg1 = "ID-123";
            var arg2 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, string, TestType, Object>(TestName, TestName, "query", testField, arg1, arg2, null);

            result.IsSuccess.Should().BeTrue();
            this.Assent(JsonConvert.SerializeObject(result.Value, Formatting.Indented), RequestBuilderAssentConfiguration);
        }

        [Test]
        public void RequestIsAsExpectedWithJsonAttributes() {
            var graphQLSchema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: CamelTestType): CamelTestResult
}
");
            var testField = graphQLSchema.Query.Fields.FirstOrDefault();
            var arg1 = "ID-123";
            var arg2 = "a string arg";
            var arg3 = new CamelTestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<CamelTestResult, string, string, CamelTestType>(TestName, TestName, "query", testField, arg1, arg2, arg3);

            result.IsSuccess.Should().BeTrue();
            this.Assent(JsonConvert.SerializeObject(result.Value, Formatting.Indented), RequestBuilderAssentConfiguration);
        }

        [Test]
        public void FailIfResultTypeNotCompatibleWithField() {
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestType, string, string, TestType>(TestName, TestName, "query", TestField, null, null, null);

            result.IsFailed.Should().BeTrue(because: $"requested result of type {nameof(TestType)} is not compatible with {nameof(TestResult)}");
        }

        [Test]
        public void FailIfResultIsNotGraphQLType() {
            var graphQLSchema = GraphQL.Types.Schema.For("type Query { aTestQuery(arg1: ID!, arg2: string, arg3: TestType): NotGraphQLModel! }");
            var testField = graphQLSchema.Query.Fields.FirstOrDefault();
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<NotGraphQLModel, string, string, TestType>(TestName, TestName, "query", TestField, null, null, null);

            result.IsFailed.Should().BeTrue(because: $"requested result of type {nameof(NotGraphQLModel)} is not a GraphQL type");
        }

        [Test]
        public void FailIfNoNullArgIsNull() {
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, string, string, TestType>(TestName, TestName, "query", TestField, null, null, null);

            result.IsFailed.Should().BeTrue(because: $"requested arg1 is ID! but supplied arg is null");
        }

        [Test]
        public void FailIfAnArgIsNotCompatibleWithField() {
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, TestType, string, TestType>(TestName, TestName, "query", TestField, null, null, null);

            result.IsFailed.Should().BeTrue(because: $"Arg1 should be ID! (string) but supplied arg is type {nameof(TestType)}");
        }

        [Test]
        public void FailIfAnArgIsNotGraphQLType() {
            var graphQLSchema = GraphQL.Types.Schema.For("type Query { aTestQuery(arg1: NotGraphQLModel): TestResult! }");
            var testField = graphQLSchema.Query.Fields.FirstOrDefault();
            var builder = new RequestBuilder();

            var result = builder.BuildRequest<TestResult, NotGraphQLModel, Object, Object>(TestName, TestName, "query", TestField, null, null, null);

            result.IsFailed.Should().BeTrue(because: $"arg1 of type {nameof(NotGraphQLModel)} is not a GraphQL type");
        }

    }
}