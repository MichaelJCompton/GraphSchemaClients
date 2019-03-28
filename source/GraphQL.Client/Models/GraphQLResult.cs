using Newtonsoft.Json.Linq;

// see https://graphql.org/learn/serving-over-http/#response
//
// result should be json like
// {
//   "data": { ... },
//   "errors": [ ... ]
// }

namespace GraphQL.Client.Models {
    public class GraphQLResult {
        public JObject Data { get; set; }
        public JArray Errors { get; set; }
    }

}