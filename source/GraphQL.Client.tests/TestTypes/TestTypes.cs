using System;
using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Client.tests.TestTypes {

    [GraphQLModel]
    public class TestType {
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