using System;
using GraphQL.Client.Attributes;

namespace GraphSchema.io.Client.Models {

    [GraphQLModel]
    public class DgraphInstance {
        public string DgraphId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Replicas { get; set; }
        public int Shards { get; set; }
        public int StorageGB { get; set; }
        public Environment Env { get; set; }
        public DgraphCertificates Certificates { get; set; }
    }
}