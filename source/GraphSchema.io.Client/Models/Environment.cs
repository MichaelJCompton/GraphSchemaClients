using System;
using GraphQL.Client.Attributes;

namespace GraphSchema.io.Client.Models {

    [GraphQLModel]
    public class Environment {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
    }

}