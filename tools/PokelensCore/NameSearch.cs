using System.Globalization;
using System.Text;

namespace PokelensCore;

/// <summary>名前検索の正規化と前方一致マッチング。<c>src/logic/name-search.js</c> の C# 移植。</summary>
/// <remarks>
/// 機能 14（パーティ編集 GUI）と機能 3（相手パーティ入力）の両方で同じ正規化規則を使うため、
/// 本クラスを <b>単一の真実源</b> として扱う（JS 版とふるまいを一致させる）。
/// ひらがな ↔ カタカナ ↔ 半角カタカナ ↔ 半角英字（ローマ字）↔ 長音記号（半角ハイフン）の正規化を行い、
/// 正規化済みクエリで前方一致を判定する。
/// </remarks>
public static class NameSearch
{
    private const int MaxResults = 10;
    private const int HiraganaMin = 0x3041;
    private const int HiraganaMax = 0x3096;
    private const int HiraganaToKatakanaOffset = 0x60;

    private static readonly HashSet<char> Vowels = new() { 'a', 'i', 'u', 'e', 'o' };

    private static readonly Dictionary<string, string> RomajiTable = new()
    {
        ["kya"] = "キャ", ["kyi"] = "キィ", ["kyu"] = "キュ", ["kye"] = "キェ", ["kyo"] = "キョ",
        ["gya"] = "ギャ", ["gyi"] = "ギィ", ["gyu"] = "ギュ", ["gye"] = "ギェ", ["gyo"] = "ギョ",
        ["sya"] = "シャ", ["syi"] = "シィ", ["syu"] = "シュ", ["sye"] = "シェ", ["syo"] = "ショ",
        ["sha"] = "シャ", ["shi"] = "シ", ["shu"] = "シュ", ["she"] = "シェ", ["sho"] = "ショ",
        ["zya"] = "ジャ", ["zyi"] = "ジィ", ["zyu"] = "ジュ", ["zye"] = "ジェ", ["zyo"] = "ジョ",
        ["jya"] = "ジャ", ["jyi"] = "ジィ", ["jyu"] = "ジュ", ["jye"] = "ジェ", ["jyo"] = "ジョ",
        ["tya"] = "チャ", ["tyi"] = "チィ", ["tyu"] = "チュ", ["tye"] = "チェ", ["tyo"] = "チョ",
        ["cha"] = "チャ", ["chi"] = "チ", ["chu"] = "チュ", ["che"] = "チェ", ["cho"] = "チョ",
        ["cya"] = "チャ", ["cyi"] = "チィ", ["cyu"] = "チュ", ["cye"] = "チェ", ["cyo"] = "チョ",
        ["dya"] = "ヂャ", ["dyi"] = "ヂィ", ["dyu"] = "ヂュ", ["dye"] = "ヂェ", ["dyo"] = "ヂョ",
        ["nya"] = "ニャ", ["nyi"] = "ニィ", ["nyu"] = "ニュ", ["nye"] = "ニェ", ["nyo"] = "ニョ",
        ["hya"] = "ヒャ", ["hyi"] = "ヒィ", ["hyu"] = "ヒュ", ["hye"] = "ヒェ", ["hyo"] = "ヒョ",
        ["bya"] = "ビャ", ["byi"] = "ビィ", ["byu"] = "ビュ", ["bye"] = "ビェ", ["byo"] = "ビョ",
        ["pya"] = "ピャ", ["pyi"] = "ピィ", ["pyu"] = "ピュ", ["pye"] = "ピェ", ["pyo"] = "ピョ",
        ["mya"] = "ミャ", ["myi"] = "ミィ", ["myu"] = "ミュ", ["mye"] = "ミェ", ["myo"] = "ミョ",
        ["rya"] = "リャ", ["ryi"] = "リィ", ["ryu"] = "リュ", ["rye"] = "リェ", ["ryo"] = "リョ",
        ["fya"] = "フャ", ["fyu"] = "フュ", ["fyo"] = "フョ",
        ["tsu"] = "ツ", ["thi"] = "ティ",

        ["ka"] = "カ", ["ki"] = "キ", ["ku"] = "ク", ["ke"] = "ケ", ["ko"] = "コ",
        ["ga"] = "ガ", ["gi"] = "ギ", ["gu"] = "グ", ["ge"] = "ゲ", ["go"] = "ゴ",
        ["sa"] = "サ", ["si"] = "シ", ["su"] = "ス", ["se"] = "セ", ["so"] = "ソ",
        ["za"] = "ザ", ["zi"] = "ジ", ["zu"] = "ズ", ["ze"] = "ゼ", ["zo"] = "ゾ",
        ["ja"] = "ジャ", ["ji"] = "ジ", ["ju"] = "ジュ", ["je"] = "ジェ", ["jo"] = "ジョ",
        ["ta"] = "タ", ["ti"] = "チ", ["tu"] = "ツ", ["te"] = "テ", ["to"] = "ト",
        ["da"] = "ダ", ["di"] = "ヂ", ["du"] = "ヅ", ["de"] = "デ", ["do"] = "ド",
        ["na"] = "ナ", ["ni"] = "ニ", ["nu"] = "ヌ", ["ne"] = "ネ", ["no"] = "ノ",
        ["ha"] = "ハ", ["hi"] = "ヒ", ["hu"] = "フ", ["he"] = "ヘ", ["ho"] = "ホ",
        ["fa"] = "ファ", ["fi"] = "フィ", ["fu"] = "フ", ["fe"] = "フェ", ["fo"] = "フォ",
        ["ba"] = "バ", ["bi"] = "ビ", ["bu"] = "ブ", ["be"] = "ベ", ["bo"] = "ボ",
        ["pa"] = "パ", ["pi"] = "ピ", ["pu"] = "プ", ["pe"] = "ペ", ["po"] = "ポ",
        ["ma"] = "マ", ["mi"] = "ミ", ["mu"] = "ム", ["me"] = "メ", ["mo"] = "モ",
        ["ya"] = "ヤ", ["yu"] = "ユ", ["yo"] = "ヨ",
        ["ra"] = "ラ", ["ri"] = "リ", ["ru"] = "ル", ["re"] = "レ", ["ro"] = "ロ",
        ["wa"] = "ワ", ["wi"] = "ウィ", ["we"] = "ウェ", ["wo"] = "ヲ",
        ["va"] = "ヴァ", ["vi"] = "ヴィ", ["vu"] = "ヴ", ["ve"] = "ヴェ", ["vo"] = "ヴォ",

        ["a"] = "ア", ["i"] = "イ", ["u"] = "ウ", ["e"] = "エ", ["o"] = "オ",
    };

    private static bool IsAsciiAlpha(char ch)
    {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private static string RomajiToKatakana(string input)
    {
        string lower = input.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length * 2);
        int i = 0;
        while (i < lower.Length)
        {
            char ch = lower[i];

            if (!IsAsciiAlpha(ch))
            {
                sb.Append(input[i]);
                i++;
                continue;
            }

            char? next = (i + 1 < lower.Length) ? lower[i + 1] : (char?)null;

            // 同子音の連続は促音 ッ + 後続子音
            if (next.HasValue && ch == next.Value && !Vowels.Contains(ch) && ch != 'n')
            {
                sb.Append('ッ');
                i++;
                continue;
            }

            if (ch == 'n' && next == 'n')
            {
                char? third = (i + 2 < lower.Length) ? lower[i + 2] : (char?)null;
                if (third.HasValue && (Vowels.Contains(third.Value) || third.Value == 'y'))
                {
                    sb.Append('ン');
                    i++;
                    continue;
                }
                sb.Append('ン');
                i += 2;
                continue;
            }

            if (ch == 'n' && (!next.HasValue || (!Vowels.Contains(next.Value) && next.Value != 'y')))
            {
                sb.Append('ン');
                i++;
                continue;
            }

            // 3 文字テーブル → 2 文字テーブル → 1 文字テーブル の順で長一致
            if (i + 3 <= lower.Length)
            {
                string three = lower.Substring(i, 3);
                if (RomajiTable.TryGetValue(three, out string? th))
                {
                    sb.Append(th);
                    i += 3;
                    continue;
                }
            }

            if (i + 2 <= lower.Length)
            {
                string two = lower.Substring(i, 2);
                if (RomajiTable.TryGetValue(two, out string? tw))
                {
                    sb.Append(tw);
                    i += 2;
                    continue;
                }
            }

            string one = ch.ToString();
            if (RomajiTable.TryGetValue(one, out string? on))
            {
                sb.Append(on);
                i++;
                continue;
            }

            // 半端な子音はスキップ
            i++;
        }
        return sb.ToString();
    }

    /// <summary>検索クエリを比較用に正規化する。</summary>
    /// <remarks>
    /// NFKC で半角カナ・濁点結合・全角英数字を一括処理し、半角ハイフンを長音記号に変換、
    /// ローマ字をカタカナへ変換、ひらがなを全角カタカナに揃える。
    /// </remarks>
    /// <param name="query">入力文字列。</param>
    /// <returns>正規化後の文字列。</returns>
    public static string NormalizeQuery(string query)
    {
        if (query.Length == 0)
        {
            return string.Empty;
        }

        // NFKC で半角カナ→全角カナ・濁点 / 半濁点の結合・全角英数字→ASCII を一括処理
        string nfkc = query.Normalize(NormalizationForm.FormKC);

        // 半角ハイフンを長音記号に置換（全角ハイフンは NFKC で半角になるためここで一緒に拾える）
        string withLongVowel = nfkc.Replace('-', 'ー');

        // ASCII 英字が含まれていればローマ字→カタカナ変換を適用
        bool hasAlpha = false;
        for (int i = 0; i < withLongVowel.Length; i++)
        {
            if (IsAsciiAlpha(withLongVowel[i])) { hasAlpha = true; break; }
        }
        string preprocessed = hasAlpha ? RomajiToKatakana(withLongVowel) : withLongVowel;

        // ひらがな → カタカナ（NFKC ではスクリプト変換されないため別途実施）
        var sb = new StringBuilder(preprocessed.Length);
        foreach (char ch in preprocessed)
        {
            int code = ch;
            if (code >= HiraganaMin && code <= HiraganaMax)
            {
                sb.Append((char)(code + HiraganaToKatakanaOffset));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    /// <summary>エントリの代表型。<c>num</c> による順序付きソート + 前方一致検索用。</summary>
    /// <param name="Name">エントリ名（マスタの日本語名）。</param>
    /// <param name="Num">図鑑番号など、ソートに使う整数キー。</param>
    public sealed record Entry(string Name, int Num);

    /// <summary>正規化したクエリでエントリ名を前方一致検索する。図鑑番号昇順に最大 10 件返す。</summary>
    /// <param name="query">検索クエリ。空文字なら空リスト。</param>
    /// <param name="entries">検索対象エントリ。</param>
    /// <returns>一致したエントリ（<c>Num</c> 昇順、最大 10 件）。</returns>
    public static IReadOnlyList<Entry> SearchByName(string query, IEnumerable<Entry> entries)
    {
        if (query.Length == 0)
        {
            return Array.Empty<Entry>();
        }
        string normalized = NormalizeQuery(query);
        var matched = new List<Entry>();
        foreach (var entry in entries)
        {
            if (NormalizeQuery(entry.Name).StartsWith(normalized, StringComparison.Ordinal))
            {
                matched.Add(entry);
            }
        }
        matched.Sort((a, b) => a.Num.CompareTo(b.Num));
        if (matched.Count > MaxResults)
        {
            return matched.GetRange(0, MaxResults);
        }
        return matched;
    }

    /// <summary>名前のみの単純な前方一致検索（順序は入力順を保つ）。<see cref="Entry"/> ではなく文字列リストを受け取る用途。</summary>
    /// <remarks>
    /// 特性・アイテム・技のサジェストでは図鑑番号が無いため本オーバーロードを使う。並び順は呼び出し元（MasterDataReader）が確定させた入力順を維持する。
    /// </remarks>
    /// <param name="query">検索クエリ。空文字なら空リスト。</param>
    /// <param name="names">検索対象の名前一覧（事前ソート済み想定）。</param>
    /// <returns>一致した名前（入力順、最大 10 件）。</returns>
    public static IReadOnlyList<string> SearchNames(string query, IEnumerable<string> names)
    {
        if (query.Length == 0)
        {
            return Array.Empty<string>();
        }
        string normalized = NormalizeQuery(query);
        var matched = new List<string>();
        foreach (string name in names)
        {
            if (NormalizeQuery(name).StartsWith(normalized, StringComparison.Ordinal))
            {
                matched.Add(name);
                if (matched.Count >= MaxResults)
                {
                    break;
                }
            }
        }
        return matched;
    }
}
