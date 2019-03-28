using System.ComponentModel.DataAnnotations;
using GraphQL.Client.Attributes;

namespace GraphSchema.io.Client.Models
{
    [GraphQLModel]
    public class EnvironmentReference
    {
        [Required]
        public string Id { get; set; }
    }
}