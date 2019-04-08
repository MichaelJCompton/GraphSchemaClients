using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GraphQL.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Client.Requests {
    public class HttpGraphQLRequestExecutor : IGraphQLRequestExecutor {

        private readonly HttpClient Client;
        private readonly IResultBuilder ResultBuilder;

        public HttpGraphQLRequestExecutor(HttpClient client, IResultBuilder resultBuilder) {
            Client = client;
            ResultBuilder = resultBuilder;
        }

        public async Task<GraphQLResult> ExecuteRequest(GraphQLRequest request) {
            // FIXME: need cancelation tokens????
            //
            // probably can also use something like polly
            // https://github.com/App-vNext/Polly to retry on transient errors??

            try {
                using(var httpContent = CreateHttpContent(request)) {
                    // Base address should be set on the http client.
                    using(var response = await Client.PostAsync("", httpContent)) {
                        using(Stream responseStream = await response.Content.ReadAsStreamAsync()) {
                            if (response.IsSuccessStatusCode) {
                                try {
                                    return DeserializeJsonFromStream<GraphQLResult>(responseStream);
                                } catch (JsonSerializationException ex) {
                                    // try rereading the stream ... is this always safe?
                                    return ResultBuilder.BuildGraphQLErrorResult(
                                        "Json Serialization Exception while reading GraphQL response",
                                        ex, request, await response.Content.ReadAsStringAsync());
                                }
                            } else {
                                return ResultBuilder.BuildGraphQLErrorResult(
                                    $"HTTP response is not success (code {response.StatusCode})",
                                    null, request, await response.Content.ReadAsStringAsync());
                            }
                        }
                    }
                }
            } catch (Exception ex) { // Just catch WebException ??
                return ResultBuilder.BuildGraphQLErrorResult(
                    "Exception while processing request",
                    ex, request, null);
            }
        }

        // see 
        // https://www.newtonsoft.com/json/help/html/Performance.htm
        // and
        // https://johnthiriet.com/efficient-api-calls/
        private T DeserializeJsonFromStream<T>(Stream stream) {
            if (stream == null || stream.CanRead == false)
                return default(T);

            using(StreamReader responseStreamReader = new StreamReader(stream)) {
                using(JsonReader jsonReader = new JsonTextReader(responseStreamReader)) {
                    JsonSerializer serializer = new JsonSerializer();

                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }

        // Built around ideas from https://johnthiriet.com/efficient-post-calls/
        private HttpContent CreateHttpContent(GraphQLRequest request) {
            HttpContent httpContent;

            var ms = new MemoryStream();
            SerializeJsonIntoStream(request, ms);
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