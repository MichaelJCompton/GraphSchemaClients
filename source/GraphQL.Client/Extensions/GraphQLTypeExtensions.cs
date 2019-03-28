using System;
using System.Reflection;
using GraphQL.Client.Attributes;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Client.Extensions {
    public static class GraphQLTypeExtensions {

        public static bool IsNonNullGraphType(this IGraphType type) {
            return type is NonNullGraphType;
        }

        public static bool IsListGraphType(this IGraphType type) {
            if (type is NonNullGraphType nonNull) {
                return nonNull.ResolvedType.IsListGraphType();
            }

            return type is ListGraphType;
        }

        public static Type GetCSNamedType(this Type type) {
            if (type.GetTypeInfo().IsGenericType) {
                return type.GenericTypeArguments[0].GetCSNamedType();
            }

            return type;
        }

        public static bool IsGraphQLType(this Type type) {

            if (type.GetTypeInfo().IsGenericType) {
                return type.GenericTypeArguments[0].IsGraphQLType();
            }
            switch (TypeNameMap(type.GetTypeInfo().Name)) {
                case "String":
                case "Int": 
                case "Float": 
                case "Boolean":
                case "DateTime":
                    return true;
                default:
                    return type.GetTypeInfo().IsDefined(typeof(GraphQLModelAttribute));
            }
        }

        private static string TypeNameMap(string csTypeName) {
            switch (csTypeName) {
                case "Int32":
                    return "Int";
                case "Single":
                    return "Float";
                default:
                    return csTypeName;
            }
        }

        public static bool IsCompatibleType(this IGraphType graphType, Type csType) {
            var refType = graphType.GetNamedType() as GraphQLTypeReference;
            if (refType != null && refType.TypeName.Equals(TypeNameMap(csType.GetCSNamedType().Name))
                || (refType.TypeName.Equals("ID") && csType.GetCSNamedType().Name.Equals("String"))) {

                if (graphType.IsListGraphType() == csType.GetTypeInfo().IsGenericType) {
                    return true;
                }
            }
            return false;
        }

        public static IType ToIType(this IGraphType graphType) {
            switch (graphType) {
                case NonNullGraphType nonNullGraphType:
                    return new NonNullType(nonNullGraphType.ResolvedType.ToIType());
                case ListGraphType listGraphType:
                    return new ListType(listGraphType.ResolvedType.ToIType());
                default:
                    var refType = graphType.GetNamedType() as GraphQLTypeReference;
                    // What if it's not a ref type?  Does it make sense?
                    // I haven't bumped into that yet.
                    return new NamedType(new NameNode(refType.TypeName));
            }
        }

        public static SelectionSet AsSelctionSet(this Type type, int depth) {
            var result = new SelectionSet();
            if (depth <= 0 || !type.GetTypeInfo().IsDefined(typeof(GraphQLModelAttribute))) {
                return result;
            }

            foreach (var property in type.GetProperties()) {
                if (!property.PropertyType.IsGraphQLType()) {
                    continue;
                }

                // ToCamelCase() is just a quick solution for my,
                // GrahpSchema-based, use-case.  Something more general might be
                // needed if others pick this up. Might be better to have some
                // sort of converter used here.
                var field = new Field(null, new NameNode(property.Name.ToCamelCase()));

                if (property.PropertyType.GetTypeInfo().IsDefined(typeof(GraphQLModelAttribute))) {
                    if (depth > 1) {
                        result.Add(field);
                        field.SelectionSet = property.PropertyType.AsSelctionSet(depth - 1);
                    }
                } else {
                    result.Add(field);
                }
            }

            return result;
        }

    }
}