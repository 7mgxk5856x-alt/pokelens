using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

/// <summary>JSON 出力に関する共通ヘルパー。</summary>
/// <remarks>成果物・キャッシュの JSON 書き出しを統一フォーマット（インデント・非エスケープ）で行うために用いる。</remarks>
internal static class JsonHelpers
{
    private static readonly JsonWriterOptions IndentedWriterOptions = new()
    {
        Indented = true,
        // 日本語など非 ASCII 文字を \uXXXX エスケープせずに出力する（成果物 JSON を人間が読めるようにするため）
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>JsonNode をインデント付き JSON 文字列に変換する。</summary>
    /// <remarks>非 ASCII 文字（日本語など）は <c>\uXXXX</c> エスケープせずそのまま出力し、成果物 JSON を人間が読めるようにする。</remarks>
    /// <param name="node">変換対象のノード。</param>
    /// <returns>インデント整形済みの JSON 文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> が null の場合。</exception>
    internal static string ToIndentedJson(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, IndentedWriterOptions);
        node.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
