using System.Collections.Generic;
using FluentResults;
using GraphQL.Types;

namespace GraphQL.Client.Schema
{
    public interface ISchemaValidator
    {
        Result ValidateAll(IEnumerable<FieldType> fields, ISchema schema);
    }
}