using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public class FieldValidator : IFieldValidator {

        private readonly ITypeValidator TypeValidator;

        public FieldValidator(ITypeValidator typeValidator) {
            TypeValidator = typeValidator;
        }

        public Result FieldTypeIsValidForSchema(FieldType field, ISchema schema) {

            var schemaField =
                schema.Query.GetField(field.Name)
                ?? schema.Mutation.GetField(field.Name);

            if (schemaField == null) {
                return Results.Fail($"{field.Name} not found in schema.");
            }

            if (!field.ResolvedType.IGraphTypeEQ(schemaField.ResolvedType)) {
                return Results.Fail($"Result type of {field.Name} does not match result type in schema.");
            }

            var finalResult = TypeValidator.IGraphQLTypeIsValidForSchema(field.ResolvedType, schema);

            foreach (var arg in field.Arguments) {
                var schemaArg = schemaField.Arguments.Find(arg.Name);

                if (schemaArg == null) {
                    finalResult = Results.Merge(
                        finalResult,
                        Results.Fail($"For {field.Name}, couldn't find schema argument matching {arg.Name}."));
                    continue;
                }

                if (!arg.ResolvedType.IGraphTypeEQ(schemaArg.ResolvedType)) {
                    finalResult = Results.Merge(
                        finalResult,
                        Results.Fail($"Argument {arg.Name} of {field.Name} does not match type in schema"));
                    continue;
                }

                finalResult = Results.Merge(
                    finalResult,
                    TypeValidator.IGraphQLTypeIsValidForSchema(arg.ResolvedType, schema));

            }
            return finalResult;
        }
    }
}