using System;
using GraphQL.Client.Attributes;
using GraphQL.Client.Models;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Client.tests.TestTypes {
    public class TestValues {

        public static string QueryName = "aTestQuery";
        public static string MutationName = "aTestMutation";

        public static string SchemaTypes = @"
type TestType {
    AnInt: Int!
    AFloat: Float
    ADouble: Float
    TheTime: DateTime
}

type TestResult {
    AntInt: Int
    Edge: TestType
}

type MissingType {
    AntInt: Int
    Edge: TestType
}

type PartBrokenType {
    AntInt: Int
    Edge: NotGraphQLModel
}

type NotGraphQLModel {
    AntInt: Int
}

enum TestEnum {
    Val1,
    Val2,
    Val3
}

input TestInputType {
    AnInt: Int!
    AFloat: Float
    ADouble: Float
    TheTime: DateTime
}";

        public static string QueryMutationString = @"

type Query {
    aTestQuery(arg1: ID!, arg2: String, arg3: TestType!): TestResult!
    anotherQuery(arg1: ID!, arg2: String, arg3: TestType!): TestType!
    yetAnotherQuery(arg1: ID!, arg2: String, arg3: TestType!): TestType!
    modelNotOkQuery: NotGraphQLModel!
    argNotModelQuery(arg1: ID!, arg2: NotGraphQLModel, arg3: TestType!): TestType!
}

type Mutation {
    aTestMutation(arg1: ID!, arg2: String, arg3: TestInputType!): TestResult!
    anotherMutation(arg1: ID!, arg2: String, arg3: TestInputType!): TestResult!
}";

        public static string Arg1 = "ID-123";
        public static string Arg2 = "a string arg";
        public static TestType Arg3 = new TestType { AnInt = 42, AFloat = 4.2f, ADouble = 4.2, TheTime = DateTime.MinValue };

        public static TestResult TestResult = new TestResult {
            AnInt = 24,
            Edge = Arg3
        };

        public static string SerializedTestResult = JsonConvert.SerializeObject(TestResult);

        public static string SerializedTestResultAsGraphQLResultString = $"{{\"Data\":{{\"{QueryName}\":{SerializedTestResult}}},\"Errors\":[]}}";

        public static GraphQLResult SerializedTestResultAsGraphQLResult = JsonConvert.DeserializeObject<GraphQLResult>(SerializedTestResultAsGraphQLResultString);
    }

    [GraphQLModel]
    public class TestType {
        public int AnInt { get; set; }
        public float AFloat { get; set; }
        public double ADouble { get; set; }
        public DateTime TheTime { get; set; }
    }

    [GraphQLModel]
    public class TestInputType {
        public int AnInt { get; set; }
        public float AFloat { get; set; }
        public double ADouble { get; set; }
        public DateTime TheTime { get; set; }
    }

    [GraphQLModel]
    public class TestResult {
        public int AnInt { get; set; }
        public TestType Edge { get; set; }
    }

    [GraphQLModel]
    public enum TestEnum {
        Val1,
        Val2,
        Val3
    }

    [GraphQLModel]
    public class PartBrokenType {
        public int AnInt { get; set; }
        public NotGraphQLModel Edge { get; set; }
    }

    public class NotGraphQLModel {
        public int AnInt { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class CamelTestResult {
        public int AnInt { get; set; }
        public TestType Edge { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class CamelTestType {
        public int AnInt { get; set; }
        public float AFloat { get; set; }
        public double ADouble { get; set; }

        [JsonProperty(PropertyName = "dayTime")]
        public DateTime TheTime { get; set; }
    }

}