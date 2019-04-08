using System;
using System.Linq;
using FluentAssertions;
using GraphQL.Client.Schema;
using GraphQL.Client.tests.TestTypes;
using NUnit.Framework;

namespace GraphQL.Client.tests.Schema {
    public class TypeValidatorFixture {

        [TestCase("String", ExpectedResult = true)]
        [TestCase("Int", ExpectedResult = true)]
        [TestCase("Float", ExpectedResult = true)]
        [TestCase("Boolean", ExpectedResult = true)]
        [TestCase("DateTime", ExpectedResult = true)]
        [TestCase("TestType", ExpectedResult = true)]
        [TestCase("TestResult", ExpectedResult = true)]
        [TestCase("TestEnum", ExpectedResult = true)]
        [TestCase("MissingType", ExpectedResult = false)]
        [TestCase("NotGraphQLModel", ExpectedResult = false)]
        [TestCase("PartBrokenType", ExpectedResult = false)]
        public bool SchemaValidatorCheck(string typeName) {
            var schema = GraphQL.Types.Schema.For(TestTypes.TestValues.SchemaTypes);

            var typeValidatorUnderTest = new TypeValidator(new [] { "GraphQL.Client.tests.TestTypes" });

            var result =
                typeValidatorUnderTest.IGraphQLTypeIsValidForSchema(
                    schema.AllTypes.First(t => string.Equals(t.Name, typeName)), schema);

            return result.IsSuccess;
        }

    }
}