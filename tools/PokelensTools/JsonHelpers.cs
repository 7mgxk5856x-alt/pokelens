using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

internal static class JsonHelpers
{
    private static readonly JsonWriterOptions IndentedWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToIndentedJson(JsonNode node)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, IndentedWriterOptions);
        node.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
