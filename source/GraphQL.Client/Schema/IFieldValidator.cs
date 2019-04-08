using FluentResults;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public interface IFieldValidator {
        Result FieldTypeIsValidForSchema(FieldType field, ISchema schema);
    }
}