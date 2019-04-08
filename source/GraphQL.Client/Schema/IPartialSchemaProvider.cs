using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Client.Schema {

    public interface IPartialSchemaProvider {
        IReadOnlyDictionary<string, FieldType> Queries { get; }
        IReadOnlyDictionary<string, FieldType> Mutations { get; }
    }

}