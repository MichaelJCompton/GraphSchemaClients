using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public class PartialSchemaProvider : IPartialSchemaProvider {

        private readonly Dictionary<string, FieldType> _queries = new Dictionary<string, FieldType>();
        private readonly Dictionary<string, FieldType> _mutations = new Dictionary<string, FieldType>();

        public IReadOnlyDictionary<string, FieldType> Queries => _queries;
        public IReadOnlyDictionary<string, FieldType> Mutations => _mutations;

        public PartialSchemaProvider() { }

        public void WithSchema(string schemaString) {
            var graphQLSchema = GraphQL.Types.Schema.For(schemaString);

            if (graphQLSchema.Query != null) {
                foreach (var field in graphQLSchema.Query.Fields) {
                    _queries[field.Name] = field;
                }
            }
            if (graphQLSchema.Mutation != null) {
                foreach (var field in graphQLSchema.Mutation.Fields) {
                    _mutations[field.Name] = field;
                }
            }
        }

    }
}