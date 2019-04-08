using System;

namespace GraphQL.Client.Attributes {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    public class GraphQLModelAttribute : Attribute {

        public GraphQLModelAttribute() { }

    }
}