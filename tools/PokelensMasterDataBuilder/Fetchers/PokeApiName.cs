using System.Text.Json.Nodes;

namespace PokelensMasterDataBuilder.Fetchers;

/// <summary>PokéAPI レスポンス JSON から日本語名・一致フォルム名を取り出す純粋ロジック。</summary>
/// <remarks>
/// HTTP 取得は <see cref="PokelensMasterDataBuilder.Fetchers.PokeAPIFetcher"/> 側が担い、ここでは取得済み JSON ノードの解釈だけを行う（副作用なし）。
/// 参照するキーは <see cref="PokeApiKey"/> に集約している。
/// </remarks>
internal static class PokeApiName
{
    private const string LangJa = "ja";                  // 日本語
    private const string LangJaHrkt = "ja-Hrkt";         // ふりがな（PokéAPI 標準表記）
    private const string LangJaHrktLower = "ja-hrkt";    // ふりがな（小文字表記の揺れに対応）

    /// <summary>species の varieties から targetSlug に一致する variety 名を探す。</summary>
    /// <remarks>双方向の前方一致（どちらかが他方の接頭辞）で判定し、複数該当する場合は最も長い名前を選ぶ。</remarks>
    /// <param name="speciesNode">pokemon-species の JSON ノード。</param>
    /// <param name="targetSlug">照合する Showdown 由来の slug。</param>
    /// <returns>一致した variety 名。無ければ null。</returns>
    internal static string? FindMatchingVariety(JsonNode speciesNode, string targetSlug)
    {
        JsonArray? varieties = speciesNode[PokeApiKey.Species.Varieties]?.AsArray();
        if (varieties == null)
        {
            return null;
        }

        string? best = null;
        int bestLen = -1;
        foreach (var v in varieties)
        {
            string? name = v?[PokeApiKey.Species.Pokemon]?[PokeApiKey.Name]?.GetValue<string>();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if ((name.StartsWith(targetSlug) || targetSlug.StartsWith(name))
                && name.Length > bestLen)
            {
                best = name;
                bestLen = name.Length;
            }
        }
        return best;
    }

    /// <summary>指定キー（names / form_names）の配列から日本語名を取り出す。</summary>
    /// <remarks>"ja" を優先し、無ければ "ja-Hrkt"（ふりがな）にフォールバックする。</remarks>
    /// <param name="root">names / form_names を含む JSON ノード。</param>
    /// <param name="arrayKey">参照する配列キー（<see cref="PokeApiKey.Names"/> または <see cref="PokeApiKey.FormNames"/>）。</param>
    /// <returns>日本語名。見つからなければ null。</returns>
    internal static string? ExtractJa(JsonNode root, string arrayKey)
    {
        JsonArray? arr = root[arrayKey]?.AsArray();
        if (arr == null)
        {
            return null;
        }

        string? jaHrkt = null;
        foreach (var entry in arr)
        {
            string? lang = entry?[PokeApiKey.Language]?[PokeApiKey.Name]?.GetValue<string>();
            string? name = entry?[PokeApiKey.Name]?.GetValue<string>();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (lang == LangJa)
            {
                return name;
            }

            if (lang == LangJaHrkt || lang == LangJaHrktLower)
            {
                jaHrkt = name;
            }
        }
        return jaHrkt;
    }
}
