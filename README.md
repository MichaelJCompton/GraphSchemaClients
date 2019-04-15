# C# Client libraries For GraphQL and GraphSchema.io.

## GraphQL

Typed client for GraphQL endpoints.

Given a schema for the GraphQL endpoint you are working with: e.g.

```
type Person {
    ...
}

input PersonInput {
    ...
}

type Query {
    getPerson(id: ID!): Person!
}

type Mutation {
    addPerson(input: PersonInput!): Person!
}
```

Annotate your model classes with `[GraphQLModel]`: e.g.

```
[GraphQLModel]
public class Person {
    ...
}

[GraphQLModel]
public class PersonInput {
    ...
}
```

Add the client into the dependency injection container with the same schema: e.g.

```
services.AddGraphQLClient<IGraphQLClient, GraphQLClient>(
    httpClient => httpClient.BaseAddress = new System.Uri("http://something/graphql"), 
    ...schema string...,
    new [] { "...models namespace..." });
```

Then you can make generic typed calls to the GraphQL backend.  The general format is

```
var result = await graphqlClient.ExecuteRequest<ResultType, Arg1Type, Arg2Type, ...>(request_name, arg1, arg2, ...)
```

E.G:

```
var result = await graphqlClient.ExecuteRequest<Person, string>("getPerson", "id-123");

var person = new PersonInput { ... };
var newPerson = await graphqlClient.ExecuteRequest<Person, PersonInput>("addPerson", person);
```

GraphQL ID type is treated as string.  C# types string, int, float, double and bool map to the GraphQL types String, Int, Float (both C# double and float) and bool.  Other types are mapped from your C# models types of the same name with the `[GraphQLModel]` attribute.

Results are returned as [FluentResults](https://github.com/altmann/FluentResults), so you can inspect success like:

```
var result = await graphqlClient.ExecuteRequest<Person, string>("getPerson", "id-123");
if(result.IsFailed) { ... deal with failure ... }
Person person = result.Value;
```

See the tests and the GraphSchema.io client for more examples.

## GraphSchema.io

Specialised version of GraphQL client for GraphSchema.io.

GraphSchema.io is a service that can host [GraphSchema](https://github.com/MichaelJCompton/GraphSchemaTools) and [Dgraph](https://github.com/dgraph-io/dgraph) instances.  It's controled via a GraphQL api.  Once you have an account, you can automate deployment of Dgraph and GraphSchema infrastructure.

Add a client for GraphSchema.io to the dependency injection container:

```
services.AddGraphSchemaIOLClient(httpClient => {
    httpClient.BaseAddress = new Uri("https://graphschema.io/api/graphql");
    httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, 
        "X-GraphSchemaIO-ApiKey ...your-key-id...:...your-key-secret...");
    });
```

Then elsewhere you'd inject the `IGraphSchemaIOClient GSioClient`.  The:

```
// Find the right environment
var envResult = await GSioClient.QueryEnvironment("Test");
if (envResult.IsFailed) { ... }
var env = envResult.Value.FirstOrDefault();

// Provision a new Dgraph instance
var dgresult = await GSioClient.AddDgraphInstanceAndWait(dgInput);

...

// Delete a Dgraph instance
await GSioClient.DeleteDgraphInstance(GSioDgraph.DgraphId);
```

You can use GraphSchema.io as part of a deployment process for your infrastructure or in automated testing.  For example, [Dgraph-dotnet](https://github.com/MichaelJCompton/Dgraph-dotnet) automated end-to-end testing spins up hosted Dgraph instances, runs end-to-end testing against the instances, and then deletes them at the end of testing.
