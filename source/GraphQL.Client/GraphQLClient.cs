using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Extensions;
using GraphQL.Client.Models;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client {
    public class GraphQLClient {

        private readonly HttpClient Client;

        private Dictionary<string, FieldType> Queries = new Dictionary<string, FieldType>();
        private Dictionary<string, FieldType> Mutations = new Dictionary<string, FieldType>();

        public GraphQLClient(HttpClient client) {
            Client = client;
        }

        // FIXME: add for testing : iscompatiblewith(schema-string)

        public void WithSchema(string schemaString) {
            var graphQLSchema = GraphQL.Types.Schema.For(schemaString);

            foreach(var field in graphQLSchema.Query.Fields) {
                Queries[field.Name] = field;
            }
            foreach(var field in graphQLSchema.Mutation.Fields) {
                Mutations[field.Name] = field;
            }
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult>(string name, string operationName = null) {
            return await ExecuteRequest<TResult, Object, Object>(name, null, null, operationName);
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult, TArg1>(string name, TArg1 arg1, string operationName = null) {
            return await ExecuteRequest<TResult, TArg1, Object>(name, arg1, null, operationName);
        }

        public async Task<Result<TResult>> ExecuteRequest<TResult, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2, string operationName = null) {
            string operationType;
            FieldType fieldType;

            var typeList = new List<Type> { typeof(TArg1), typeof(TArg2) };
            var argList = new List<object> { arg1, arg2 };

            if (string.IsNullOrWhiteSpace(name)) {
                return Results.Fail<TResult>(new FluentResults.ExceptionalError(new ArgumentNullException(nameof(name))));
            }

            operationName = string.IsNullOrWhiteSpace(operationName) ? name : operationName;

            if (Queries.TryGetValue(name, out fieldType)) {
                operationType = "query";
            } else if (Mutations.TryGetValue(name, out fieldType)) {
                operationType = "mutation";
            } else {
                return Results.Fail<TResult>(new FluentResults.ExceptionalError(new KeyNotFoundException($"No query or mutation \"{name}\" was found.")));
            }

            if (!fieldType.ResolvedType.IsCompatibleType(typeof(TResult))) {
                var refType = fieldType.ResolvedType.GetNamedType() as GraphQLTypeReference;
                var typename = refType?.TypeName ?? "unknown type";
                return Results.Fail<TResult>($"Result type {typeof(TResult).Name} doesn't match GraphQL result type {typename}");
            }

            if(!typeof(TResult).IsGraphQLType()) {
                return Results.Fail<TResult>($"Result type {typeof(TResult).Name} doesn't have GraphQLModel attribute");
            }

            VariableDefinitions variableDefinitions = new VariableDefinitions();
            JObject variables = new JObject();
            Arguments arguments = new Arguments();

            var fieldArgs = fieldType.Arguments.ToList();
            for (int i = 0; i < fieldArgs.Count; i++) {
                var fieldArg = fieldArgs[i];
                var argType = typeList[i];
                var arg = argList[i];

                if (fieldArg.ResolvedType.IsNonNullGraphType() && arg == null) {
                    return Results.Fail<TResult>(new FluentResults.ExceptionalError(new ArgumentNullException(nameof(name), $"Argument is null, but GraphQL is requiring {fieldArg.Name} to be non-null.")));
                }

                if (!fieldArg.ResolvedType.IsCompatibleType(argType)) {
                    var refType = fieldArg.ResolvedType.GetNamedType() as GraphQLTypeReference;
                    var typename = refType?.TypeName ?? "unknown type";
                    return Results.Fail<TResult>($"Argument type {argType.Name} doesn't match GraphQL type {typename}");
                }

                if(!argType.IsGraphQLType()) {
                    return Results.Fail<TResult>($"Argument type {argType.Name} doesn't have GraphQLModel attribute");
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

            var result = await ExecuteRequest(
                operationType,
                operationName,
                variableDefinitions,
                requestField,
                variables);

            if (result.Errors.Count == 0) {
                TResult asTResult = result.Data[name].ToObject<TResult>();
                if(fieldType.ResolvedType.IsNonNullGraphType() && asTResult == null) {
                    return Results.Fail<TResult>($"Returned null, but GraphQL required {fieldType.ResolvedType.GetNamedType().Name} to be non-null.");
                }
                return Results.Ok(asTResult);
            }
            
            return Results.Fail<TResult>(result.Errors.ToString());
        }

        public async Task<GraphQLResult> ExecuteRequest(
            string operationType,
            string operationName,
            VariableDefinitions variableDefinitions,
            ISelection selection,
            JObject variables
        ) {

            var formattedVariableDefinitions = variableDefinitions.Any()
                ? "(" + string.Join(", ", variableDefinitions.Select(AstPrinter.Print)) + ")"
                : "";

            GraphQLRequest request = new GraphQLRequest {
                Query = $"{operationType} {operationName}{formattedVariableDefinitions} {{\n {AstPrinter.Print(selection)} \n}}",
                OperationName = operationName,
                Variables = variables
            };

            return await ExecuteRequest(request);
        }

        // Built around ideas from https://johnthiriet.com/efficient-post-calls/
        public async Task<GraphQLResult> ExecuteRequest(GraphQLRequest request) {

            using(var httpContent = CreateHttpContent(request)) {

                // Base address should be set on the client from configuration.
                using(var response = await Client.PostAsync("", httpContent)) // FIXME: need cancelation tokens????
                {
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<GraphQLResult>(responseContent);
                }
            }

            // FIXME: what to do with errors in here??? maybe should add them to
            // the errors payload of the result and return that ... that'll mean
            // a client has to deal with errors from this and from the backend
            // this is calling 
            //
            // try { } catch() { } and return error if we get to the end.
            //
            // probably can also use something like polly
            // https://github.com/App-vNext/Polly to retry on transient errors??
        }

        private HttpContent CreateHttpContent(GraphQLRequest query) {
            HttpContent httpContent;

            var ms = new MemoryStream();
            SerializeJsonIntoStream(query, ms);
            ms.Seek(0, SeekOrigin.Begin);
            httpContent = new StreamContent(ms, 1024);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return httpContent;
        }

        private void SerializeJsonIntoStream(object value, Stream stream) {
            using(var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true)) {
                using(var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None }) {
                    var js = new JsonSerializer();
                    js.Serialize(jtw, value);
                    jtw.Flush();
                }
            }
        }

    }
}