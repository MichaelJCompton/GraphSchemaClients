using FluentResults;

namespace GraphQL.Client.Models
{
    public class GraphQLErrorReason : Error
    {
        public readonly GraphQLError GraphQLError;
        public GraphQLErrorReason(GraphQLError graphQLError) {
            GraphQLError = graphQLError;
        }
        
    }
}