using System;
using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphSchema.io.Client.Models {

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class DgraphInstance {
        public string Id { get; set; }
        public string DgraphId { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Replicas { get; set; }
        public int Shards { get; set; }
        public int StorageGB { get; set; }
        public Environment Env { get; set; }
        public DgraphCertificates Certificates { get; set; }
    }
}
