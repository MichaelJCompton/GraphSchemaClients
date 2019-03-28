using System;

namespace GraphQL.Client.Attributes {

    [AttributeUsage(AttributeTargets.Class)]
    public class GraphQLModelAttribute : Attribute {

        public GraphQLModelAttribute() { }

    }
}