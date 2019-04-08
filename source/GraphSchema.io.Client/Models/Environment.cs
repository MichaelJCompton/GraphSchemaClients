using System;
using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphSchema.io.Client.Models {

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class Environment {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
    }

}