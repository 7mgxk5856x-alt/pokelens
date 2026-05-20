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
        // 日本語など非 ASCII 文字を \uXXXX エスケープせずに出力する（成果物 JSON を人間が読めるようにするため）
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToIndentedJson(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, IndentedWriterOptions);
        node.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
