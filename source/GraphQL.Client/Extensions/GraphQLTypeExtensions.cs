using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentResults;
using GraphQL.Client.Attributes;
using GraphQL.Language.AST;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Client.Extensions {
    public static class GraphQLTypeExtensions {

        /* fixformat ignore:start */
        private static IDictionary<string, NamingStrategy> NamingStrategies =
            new Dictionary<string, NamingStrategy> { 
                { nameof(DefaultNamingStrategy), new DefaultNamingStrategy() },
                { nameof(CamelCaseNamingStrategy), new CamelCaseNamingStrategy() },
                { nameof(SnakeCaseNamingStrategy), new SnakeCaseNamingStrategy() }
            };
        /* fixformat ignore:end */

        public static bool IGraphTypeEQ(this IGraphType a, IGraphType b) {
            if (a is NonNullGraphType aNN && b is NonNullGraphType bNN) {
                return aNN.ResolvedType.IGraphTypeEQ(bNN.ResolvedType);
            } else if (a is ListGraphType aL && b is ListGraphType bL) {
                return aL.ResolvedType.IGraphTypeEQ(bL.ResolvedType);
            } else {
                var anamed = a.GetName();
                var bnamed = b.GetName();
                return string.Equals(a.GetName(), b.GetName());
            }
        }

        public static string GetName(this IGraphType type) {
            switch (type) {
                case ObjectGraphType obj:
                    return obj.Name;
                case InputObjectGraphType inp:
                    return inp.Name;
                case GraphQLTypeReference refType:
                    return refType.TypeName;
                case ScalarGraphType scalar:
                    return scalar.Name;
                default:
                    return null;
            }
        }

        public static bool IsNonNullGraphType(this IGraphType type) {
            return type is NonNullGraphType;
        }

        public static bool IsListGraphType(this IGraphType type) {
            if (type is NonNullGraphType nonNull) {
                return nonNull.ResolvedType.IsListGraphType();
            }

            return type is ListGraphType;
        }

        public static bool IsSupportedScalarGraphType(this IGraphType type) {
            switch (type.GetName()) {
                case "ID":
                case "String":
                case "Int":
                case "Float":
                case "Boolean":
                case "DateTime":
                    return true;
                default:
                    return false;
            }
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
                case "Double":
                    return "Float";
                default:
                    return csTypeName;
            }
        }

        public static bool IsScalarGraphQLType(this Type type) {
            switch (TypeNameMap(type.GetTypeInfo().Name)) {
                case "String":
                case "Int":
                case "Float":
                case "Boolean":
                case "DateTime":
                    return true;
                default:
                    return false;
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

        private static NamingStrategy GetNamingStratgey(this Type type) {
            var objectAttribute = type.GetTypeInfo().GetCustomAttributes<JsonObjectAttribute>(false).FirstOrDefault();
            return objectAttribute?.NamingStrategyType == null
                ? NamingStrategies[nameof(DefaultNamingStrategy)]
                : NamingStrategies[objectAttribute.NamingStrategyType.Name];
        }

        private static string ResolvePropertyName(this PropertyInfo propInfo, NamingStrategy namingStrategy) {
            var propertyAttribute = propInfo.GetCustomAttributes<JsonPropertyAttribute>(false).FirstOrDefault();

            string mappedName;
            bool hasSpecifiedName;
            if (propertyAttribute?.PropertyName != null) {
                mappedName = propertyAttribute.PropertyName;
                hasSpecifiedName = true;
            } else {
                mappedName = propInfo.Name;
                hasSpecifiedName = false;
            }

            return namingStrategy.GetPropertyName(mappedName, hasSpecifiedName);
        }

        public static SelectionSet AsSelctionSet(this Type type, int depth) {
            var result = new SelectionSet();
            if (depth <= 0 || !type.GetTypeInfo().IsDefined(typeof(GraphQLModelAttribute))) {
                return result;
            }

            NamingStrategy namingStrategy = type.GetNamingStratgey();

            foreach (var property in type.GetProperties()) {
                if (!property.PropertyType.IsGraphQLType()) {
                    continue;
                }

                var field = new Field(null, new NameNode(property.ResolvePropertyName(namingStrategy)));

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