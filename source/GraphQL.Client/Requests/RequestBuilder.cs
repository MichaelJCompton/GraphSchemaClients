using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Client.Models;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client.Requests {
    public class RequestBuilder : IRequestBuilder {

        public RequestBuilder() { }

        // Pre: name and operationName are both non-null and not empty
        // Pre: fieldType is non null
        public Result<GraphQLRequest> BuildRequest<TResult, TArg1, TArg2, TArg3>(
            string name,
            string operationName,
            string operationType,
            FieldType fieldType,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3
        ) {

            var typeList = new List<Type> { typeof(TArg1), typeof(TArg2), typeof(TArg3) };
            var argList = new List<object> { arg1, arg2, arg3 };

            if (!fieldType.ResolvedType.IsCompatibleType(typeof(TResult))) {
                var refType = fieldType.ResolvedType.GetNamedType() as GraphQLTypeReference;
                var typename = refType?.TypeName ?? "unknown type";
                return Results.Fail<GraphQLRequest>($"Result type {typeof(TResult).Name} doesn't match GraphQL result type {typename}");
            }

            if (!typeof(TResult).IsGraphQLType()) {
                return Results.Fail<GraphQLRequest>($"Result type {typeof(TResult).Name} doesn't have GraphQLModel attribute");
            }

            VariableDefinitions variableDefinitions = new VariableDefinitions();
            JObject variables = new JObject();
            Arguments arguments = new Arguments();

            var fieldArgs = fieldType.Arguments.ToList();
            for (int i = 0; i < fieldArgs.Count; i++) {
                var fieldArg = fieldArgs[i];
                var argType = typeList[i];
                var arg = argList[i];

                if (arg == null) {
                    if (fieldArg.ResolvedType.IsNonNullGraphType() && arg == null) {
                        return Results.Fail<GraphQLRequest>(
                            new FluentResults.ExceptionalError(
                                new ArgumentNullException(nameof(name), $"Argument is null, but GraphQL is requiring {fieldArg.Name} to be non-null.")));
                    }
                    continue;
                }

                if (!fieldArg.ResolvedType.IsCompatibleType(argType)) {
                    var refType = fieldArg.ResolvedType.GetNamedType() as GraphQLTypeReference;
                    var typename = refType?.TypeName ?? "unknown type";
                    return Results.Fail<GraphQLRequest>($"Argument type {argType.Name} doesn't match GraphQL type {typename}");
                }

                if (!argType.IsGraphQLType()) {
                    return Results.Fail<GraphQLRequest>($"Argument type {argType.Name} doesn't have GraphQLModel attribute");
                }

                arguments.Add(new Argument(new NameNode(fieldArg.Name)) {
                    Value = new VariableReference(new NameNode(fieldArg.Name))
                });

                variableDefinitions.Add(new VariableDefinition(new NameNode(fieldArg.Name)) {
                    Type = fieldArg.ResolvedType.ToIType()
                });

                variables[fieldArg.Name] = JToken.FromObject(arg);
            }

            // FIXME: pick up the depth from the model type
            SelectionSet selections = typeof(TResult).GetCSNamedType().AsSelctionSet(3);

            var requestField = new Field(null, new NameNode(name)) {
                Arguments = arguments,
                Directives = new Directives(),
                SelectionSet = selections
            };

            return Results.Ok(AstToRequest(
                operationType,
                operationName,
                variableDefinitions,
                requestField,
                variables));
        }

        public GraphQLRequest AstToRequest(
            string operationType,
            string operationName,
            VariableDefinitions variableDefinitions,
            ISelection selection,
            JObject variables
        ) {

            var formattedVariableDefinitions = variableDefinitions.Any()
                ? "(" + string.Join(", ", variableDefinitions.Select(AstPrinter.Print)) + ")"
                : "";

            return new GraphQLRequest {
                Query = $"{operationType} {operationName}{formattedVariableDefinitions} {{\n {AstPrinter.Print(selection)} \n}}",
                    OperationName = operationName,
                    Variables = variables
            };
        }

    }
}