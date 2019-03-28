using System.ComponentModel.DataAnnotations;

namespace GraphSchema.io.Client.Models
{
    public class GraphSchemaIOConnection
    {
        [Required, Url]
        public string Endpoint { get; set; } = "https://graphschema.io/api/graphql";

        [Required]
        public string ApiKeyId { get; set; }

        [Required]
        public string ApiKeySecret { get; set; }
    }
}