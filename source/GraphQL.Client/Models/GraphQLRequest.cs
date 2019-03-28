using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

// see https://graphql.org/learn/serving-over-http/
//
// Get:
// http://myapi/graphql?query={me{name}}
//
// Post:
// Should have a json content body like
// {
//   "query": "...",
//   "operationName": "...",
//   "variables": { "myVariable": "someValue", ... }
// }

namespace GraphQL.Client.Models {
    public class GraphQLRequest {
        [Required]
        public string Query { get; set; }
        public string OperationName { get; set; }
        public JObject Variables { get; set; }
    }
}