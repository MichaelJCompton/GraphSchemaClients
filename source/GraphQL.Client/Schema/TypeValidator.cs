using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Types;

namespace GraphQL.Client.Schema {
    public class TypeValidator : ITypeValidator {

        private readonly List<string> ModelNamespaces;

        public TypeValidator(IEnumerable<string> modelNamespaces) {
            ModelNamespaces = modelNamespaces.ToList();
        }

        public Result IGraphQLTypeIsValidForSchema(IGraphType type, ISchema schema) {
            var namedType = type.GetNamedType();

            if(namedType.IsSupportedScalarGraphType()) {
                return Results.Ok();
            }

            var csType = GetType(namedType.GetName());
            if (csType != null) {
                return TypeIsValidForSchema(csType, schema);
            }
            return Results.Fail($"Type {namedType.GetName()} can't be found as a C# type.");
        }

        private Type GetType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(t => t.GetTypes()).Where(t =>
                String.Equals(t.Name, name, StringComparison.Ordinal)
                && ModelNamespaces.Contains(t.Namespace)).FirstOrDefault();

        private Result TypeIsValidForSchema(Type type, ISchema schema) {
            if (!type.IsGraphQLType()) {
                return Results.Fail($"Type {type.Name} isn't a GraphQL type.");
            }

            var finalResult = Results.Ok();
            foreach (var property in type.GetProperties()) {
                if (property.PropertyType.IsScalarGraphQLType()) {
                    continue;
                }

                if (schema.AllTypes.Any(t => string.Equals(t.Name, property.PropertyType.Name))) {
                    var result = TypeIsValidForSchema(property.PropertyType, schema);
                    finalResult = Results.Merge(finalResult, result);
                }
                // probably should be saying if it's not in the schema, 
                // then it should be marked with ignore
            }

            return finalResult;
        }

    }
}