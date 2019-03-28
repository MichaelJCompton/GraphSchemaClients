using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace GraphSchema.io.Client.Resources
{
    public class ResourceProvider {

        public static string GetSchemaFragment() {
            return ReadResourceAsString("GraphSchema.io.schema");
        }

        public static string ReadResourceAsString(string resourceName) {
            string[] names = Assembly.GetAssembly(typeof(ResourceProvider)).GetManifestResourceNames();
            IFileProvider embeddedProvider = new EmbeddedFileProvider(Assembly.GetAssembly(typeof(ResourceProvider)), "GraphSchema.io.Client.Resources");
            using(var stream = embeddedProvider.GetFileInfo(resourceName).CreateReadStream()) {
                using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                    return reader.ReadToEnd();
                }
            }

        }
    }
}