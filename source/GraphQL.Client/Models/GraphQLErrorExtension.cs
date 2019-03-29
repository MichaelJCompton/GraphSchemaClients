using System;

namespace GraphQL.Client.Models {
    public class GraphQLErrorExtension {
        /// <summary>
        /// Unique Id.  Intended for cross reference with logs etc.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Set if an exception caused the error.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// If the error occured while processing the http response from the
        /// GraphQL server, then the text of the response is set here.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// GraphQL request that was sent to the GraphQL server.
        /// </summary>
        public GraphQLRequest Request { get; set; }

        /// <summary>
        /// Timestamp that the client minted the error.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}