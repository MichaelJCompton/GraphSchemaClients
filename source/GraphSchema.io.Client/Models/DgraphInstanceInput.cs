using GraphQL.Client.Attributes;

namespace GraphSchema.io.Client.Models
{
    [GraphQLModel]
    public class DgraphInstanceInput
    {
        public int Replicas { get; set; }
        public int Shards { get; set; }
        public int StorageGB { get; set; }
        public EnvironmentReference Env { get; set; }
    }
}