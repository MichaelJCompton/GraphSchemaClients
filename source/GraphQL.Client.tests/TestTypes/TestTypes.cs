using System;
using GraphQL.Client.Attributes;

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
    
}