using System.Linq;
using FluentAssertions;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Client.Schema;
using GraphQL.Types;
using NSubstitute;
using NUnit.Framework;

namespace GraphQL.Client.tests.Schema {
    public class FieldValidatorFixture {


        ISchema Schema;

        [OneTimeSetUp]
        public void SetSchema() {
            Schema = GraphQL.Types.Schema.For(TestTypes.TestValues.SchemaTypes + TestTypes.TestValues.QueryMutationString);
        }

        [Test]
        public void FieldNotInSchemaIsError() {
            var schemaWithMissingField = GraphQL.Types.Schema.For(@"
type Query { 
    aMissingQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult! 
}");
            var missingField = schemaWithMissingField.Query.GetField("aMissingQuery");
            var fieldValidatorUnderTest = new FieldValidator(Substitute.For<ITypeValidator>());

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(missingField, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void DifferentResultTypesIsError() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestType!
}");
            var field = schema.Query.GetField("aTestQuery");
            var fieldValidatorUnderTest = new FieldValidator(Substitute.For<ITypeValidator>());

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void ResultFailsTypeValidationIsError() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    modelNotOkQuery: NotGraphQLModel!
}");
            var field = schema.Query.GetField("modelNotOkQuery");
            var typeValidator = Substitute.For<ITypeValidator>();
            typeValidator.IGraphQLTypeIsValidForSchema(Arg.Any<IGraphType>(), Arg.Any<ISchema>()).Returns(Results.Fail("Error"));
            var fieldValidatorUnderTest = new FieldValidator(typeValidator);

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void MissingArgsError() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, notInSchema: String, arg3: TestType!): TestResult!
}");
            var field = schema.Query.GetField("aTestQuery");
            var typeValidator = Substitute.For<ITypeValidator>();
            typeValidator.IGraphQLTypeIsValidForSchema(Arg.Any<IGraphType>(), Arg.Any<ISchema>()).Returns(Results.Ok());
            var fieldValidatorUnderTest = new FieldValidator(typeValidator);

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void ArgsTypeDifferenceIsError() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: TestType, arg3: TestType!): TestResult!
}");
            var field = schema.Query.GetField("aTestQuery");
            var typeValidator = Substitute.For<ITypeValidator>();
            typeValidator.IGraphQLTypeIsValidForSchema(Arg.Any<IGraphType>(), Arg.Any<ISchema>()).Returns(Results.Ok());
            var fieldValidatorUnderTest = new FieldValidator(typeValidator);

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void ArgsFailesTypeValidationIsError() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    argNotModelQuery(arg1: ID!, arg2: NotGraphQLModel, arg3: TestType!): TestType!
}");
            var field = schema.Query.GetField("argNotModelQuery");
            var typeValidator = Substitute.For<ITypeValidator>();
            typeValidator.IGraphQLTypeIsValidForSchema(Arg.Any<IGraphType>(), Arg.Any<ISchema>())
                .Returns(x => (string.Equals(((IGraphType) x[0]).GetName(), "NotGraphQLModel"))
                    ? Results.Fail("Error") : Results.Ok());
            var fieldValidatorUnderTest = new FieldValidator(typeValidator);

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsFailed.Should().BeTrue();
        }

        [Test]
        public void MatchingFieldIsOK() {
            var schema = GraphQL.Types.Schema.For(@"
type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
}");
            var field = schema.Query.GetField("aTestQuery");
            var typeValidator = Substitute.For<ITypeValidator>();
            typeValidator.IGraphQLTypeIsValidForSchema(Arg.Any<IGraphType>(), Arg.Any<ISchema>()).Returns(Results.Ok());
            var fieldValidatorUnderTest = new FieldValidator(typeValidator);

            var result = fieldValidatorUnderTest.FieldTypeIsValidForSchema(field, Schema);

            result.IsSuccess.Should().BeTrue();
        }

    }
}