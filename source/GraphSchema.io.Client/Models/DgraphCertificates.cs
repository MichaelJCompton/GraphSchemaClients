using GraphQL.Client.Attributes;

namespace GraphSchema.io.Client.Models
{
    [GraphQLModel]
    public class DgraphCertificates
    {
        public string CaCert { get; set; }
        public string ClientCert { get; set; }
        public string ClientKey { get; set; }
    }
}
