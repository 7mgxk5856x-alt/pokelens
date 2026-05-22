using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

/// <summary>JSON 出力に関する共通ヘルパー。</summary>
internal static class JsonHelpers
{
    private static readonly JsonWriterOptions IndentedWriterOptions = new()
    {
        Indented = true,
        // 日本語など非 ASCII 文字を \uXXXX エスケープせずに出力する（成果物 JSON を人間が読めるようにするため）
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>JsonNode をインデント付き JSON 文字列に変換する。非 ASCII 文字はエスケープせずそのまま出力する。</summary>
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
