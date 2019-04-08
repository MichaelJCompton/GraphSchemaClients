using FluentResults;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public interface ITypeValidator {
        Result IGraphQLTypeIsValidForSchema(IGraphType type, ISchema schema);
    }
}