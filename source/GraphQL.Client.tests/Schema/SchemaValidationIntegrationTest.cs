using System.Linq;
using Assent;
using Assent.Namers;
using FluentAssertions;
using GraphQL.Client.Requests;
using GraphQL.Client.Schema;
using NSubstitute;
using NUnit.Framework;

namespace GraphQL.Client.tests.Schema {
    public class SchemaValidationIntegrationTest {

        private string ValidSchemaSubset = @"

type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
    anotherQuery(arg1: ID!, arg2: String, arg3: TestType!): TestType!
}

type Mutation {
    aTestMutation(arg1: ID!, arg2: String, arg3: TestInputType!): TestResult!
}";


        public string InvalidSchema = @"

type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
    anotherQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
    yetAnotherQuery(arg1: ID!, arg2: TestType, arg3: TestType!): TestType!
    modelNotOkQuery: NotGraphQLModel!
    argNotModelQuery(arg1: ID!, arg2: NotGraphQLModel, arg3: TestType!): TestType!
}

type Mutation {
    aTestMutation(arg1: ID!, arg2: String, arg3: TestInputType!): TestResult!
    aMissingMutation(arg1: ID!, arg2: String): TestResult! 
    anotherMutation(arg1: ID!, arg2: String, arg3456: TestInputType!): TestResult!
}";

        #region ValidSchema

        [Test]
        public void AValidSchemaIsOK() {
            var client = BuildTestClient(ValidSchemaSubset);

            var result = client.ValidateAgainstSchema(TestTypes.TestValues.SchemaTypes + TestTypes.TestValues.QueryMutationString);
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region InvalidSchema

        [Test]
        public void InvalidSchemaReportsAllErrors() {
            Assent.Configuration assentConfiguration;
            assentConfiguration = new Assent.Configuration()
                .UsingNamer(new SubdirectoryNamer("AsSelectionSetApproved"));

            var client = BuildTestClient(InvalidSchema);

            var result = client.ValidateAgainstSchema(TestTypes.TestValues.SchemaTypes + TestTypes.TestValues.QueryMutationString);
            result.IsFailed.Should().BeTrue();
            this.Assent(string.Join("\n", result.Errors.Select(e => e.Message)), assentConfiguration);
        }

        #endregion

        private GraphQLClient BuildTestClient(string schemaSubset) {
            var partialSchemaProvider = new PartialSchemaProvider();
            partialSchemaProvider.WithSchema(schemaSubset);

            var schemaValidator = new SchemaValidator(new FieldValidator(new TypeValidator(new [] { "GraphQL.Client.tests.TestTypes" })));

            return new GraphQLClient(
                Substitute.For<IGraphQLRequestExecutor>(),
                Substitute.For<IRequestBuilder>(),
                Substitute.For<IResultBuilder>(),
                partialSchemaProvider,
                schemaValidator);
        }

    }
}