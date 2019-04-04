using System.ComponentModel.DataAnnotations;
using GraphQL.Client.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphSchema.io.Client.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [GraphQLModel]
    public class EnvironmentReference
    {
        [Required]
        public string Id { get; set; }
    }
}