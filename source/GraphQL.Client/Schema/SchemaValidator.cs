using System.Collections.Generic;
using FluentResults;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public class SchemaValidator : ISchemaValidator {

        private readonly IFieldValidator FieldValidator;

        public SchemaValidator(IFieldValidator fieldValidator) {
            FieldValidator = fieldValidator;
        }

        public Result ValidateAll(IEnumerable<FieldType> fields, ISchema schema) {
            var finalResult = Results.Ok();

            foreach (var field in fields) {
                finalResult = Results.Merge(finalResult, FieldValidator.FieldTypeIsValidForSchema(field, schema));
            }

            return finalResult;
        }
        
    }
}