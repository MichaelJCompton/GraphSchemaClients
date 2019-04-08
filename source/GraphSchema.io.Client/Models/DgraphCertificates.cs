using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphSchema.io.Client.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class DgraphCertificates
    {
        public string CaCert { get; set; }
        public string ClientCert { get; set; }
        public string ClientKey { get; set; }
    }
}
