using System;

namespace GraphQL.Client.Models {

    /// <summary>
    /// Error type returned by the GraphQL client.  See spec at
    /// https://graphql.github.io/graphql-spec/June2018/#sec-Errors
    ///
    /// The GraphQL server contacted by the client might return it's own errors.
    /// The client doesn't inspect or alter those, it just passes them through.
    /// GraphQLError type is returned when an error occurs within the client.
    /// </summary>
    public class GraphQLError {
        /// <summary>
        /// Error message.  Always set and required by spec.
        /// </summary>
        public string Message { get; set; }

        // locations ... not needed here?  That should come from the
        // server the request is sent to if there is an error there.
        //
        // paths ... as above

        /// <summary>
        /// Extensions to the error type.
        /// </summary>
        public GraphQLErrorExtension Extensions { get; set; }
    }
}