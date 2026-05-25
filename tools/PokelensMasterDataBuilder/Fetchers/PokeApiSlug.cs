using System.Text;

namespace PokelensMasterDataBuilder.Fetchers;

/// <summary>Showdown の名前を PokéAPI のリソース slug に変換する。</summary>
/// <remarks>
/// PokéAPI はハイフン区切りの小文字 slug でリソースを引くため、Showdown 名（大文字・空白・約物・アクセント付き
/// 文字を含む）を正規化する。アイテムとポケモンフォルムで除去する文字がわずかに異なる（ポケモン名のみ '%' を除去）。
/// </remarks>
internal static class PokeApiSlug
{
    /// <summary>Showdown のアイテム名（例: "Choice Scarf", "Wellspring Mask"）を PokéAPI のアイテム slug に変換する。</summary>
    /// <remarks>小文字化し、空白・アンダースコアをハイフンに、アクセント付き e を e に正規化し、約物を除去する。</remarks>
    /// <param name="showdownName">Showdown のアイテム名。</param>
    /// <returns>PokéAPI のアイテム slug。</returns>
    internal static string ItemSlug(string showdownName) => Normalize(showdownName, stripPercent: false);

    /// <summary>Showdown のポケモン名（例: "Rotom-Wash", "Mr. Mime", "Flabébé"）を PokéAPI のフォルム slug に変換する。</summary>
    /// <remarks>小文字化し、空白・アンダースコアをハイフンに、アクセント付き e を e に正規化し、約物（%含む）を除去する。</remarks>
    /// <param name="showdownName">Showdown のポケモン名。</param>
    /// <returns>PokéAPI のフォルム slug。</returns>
    internal static string PokemonFormSlug(string showdownName) => Normalize(showdownName, stripPercent: true);

    // 共通の slug 正規化。約物（' ’ . : ,）を除去し、空白・アンダースコアをハイフンに、
    // アクセント付き e を e に変換する。ポケモン名のみ '%' も除去する（例: "Zygarde-10%" → "zygarde-10"）。
    private static string Normalize(string showdownName, bool stripPercent)
    {
        string lower = showdownName.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            if (c == '\'' || c == '’' || c == '.' || c == ':' || c == ',' || (stripPercent && c == '%'))
            {
                continue;
            }

            if (c == ' ' || c == '_')
            {
                sb.Append('-');
            }
            else if (c == 'é' || c == 'è' || c == 'ê' || c == 'ë')
            {
                sb.Append('e');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
