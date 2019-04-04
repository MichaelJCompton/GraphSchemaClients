using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphSchema.io.Client.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class DgraphInstanceInput
    {
        public int Replicas { get; set; }
        public int Shards { get; set; }
        public int StorageGB { get; set; }
        public EnvironmentReference Env { get; set; }
    }
}