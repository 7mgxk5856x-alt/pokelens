using System.Text.Json;
using PokelensCore.Models;

namespace PokelensCore;

/// <summary>data/*.json を読み込んで <see cref="MasterDataSnapshot"/> に集約するリーダー。</summary>
/// <remarks>
/// PartyEditor のサジェスト・バリデーション、および将来のフロント連携向け共通基盤として使う。
/// 並び順:
/// - ポケモン名: pokedex.json の `num` 昇順
/// - 特性 / アイテム / 技: マスタ JSON のキー順を入力順（五十音順想定）として保持
/// </remarks>
public static class MasterDataReader
{
    /// <summary>各マスタを <see cref="DataPaths.Master"/> から読み込んでスナップショットに集約する。</summary>
    /// <returns>マスターデータスナップショット。</returns>
    /// <exception cref="FileNotFoundException">いずれかのマスタが存在しない場合。</exception>
    /// <exception cref="JsonException">マスタの JSON 形式が不正な場合。</exception>
    public static MasterDataSnapshot Load()
    {
        return new MasterDataSnapshot
        {
            PokemonNames = LoadPokemonNames(DataPaths.Master.Pokedex()),
            AbilityNames = LoadKeys(DataPaths.Master.Abilities()),
            ItemNames = LoadKeys(DataPaths.Master.Items()),
            MoveNames = LoadKeys(DataPaths.Master.Moves()),
            NatureNames = LoadKeys(DataPaths.Master.Natures()),
            NatureModifiers = LoadNatureModifiers(DataPaths.Master.Natures()),
        };
    }

    /// <summary>pokedex.json から <c>num</c> 昇順でポケモン名一覧を取り出す。</summary>
    internal static IReadOnlyList<string> LoadPokemonNames(string pokedexPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(pokedexPath));
        var entries = new List<(int Num, string Name)>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;
            if (!prop.Value.TryGetProperty("num", out var numEl)) continue;
            if (!prop.Value.TryGetProperty("name", out var nameEl)) continue;
            if (numEl.ValueKind != JsonValueKind.Number || nameEl.ValueKind != JsonValueKind.String) continue;
            entries.Add((numEl.GetInt32(), nameEl.GetString()!));
        }
        entries.Sort((a, b) => a.Num.CompareTo(b.Num));
        return entries.Select(e => e.Name).ToList();
    }

    /// <summary>オブジェクトのトップレベルキーを入力順で取り出す（abilities / items / moves / natures 共通）。</summary>
    internal static IReadOnlyList<string> LoadKeys(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var keys = new List<string>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            keys.Add(prop.Name);
        }
        return keys;
    }

    /// <summary>natures.json から性格 → 補正情報を読む。各エントリは <c>modifiers: { atk: 1.1, spa: 0.9 }</c> の形式。</summary>
    /// <remarks>
    /// 1.1 のステータスを上昇、0.9 のステータスを下降として記録する。補正なし性格（空 <c>modifiers</c>）は両 null になる。
    /// </remarks>
    internal static IReadOnlyDictionary<string, NatureModifiers> LoadNatureModifiers(string naturesPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(naturesPath));
        var result = new Dictionary<string, NatureModifiers>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            string? up = null;
            string? down = null;
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("modifiers", out var modsEl) &&
                modsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var stat in modsEl.EnumerateObject())
                {
                    if (stat.Value.ValueKind != JsonValueKind.Number) continue;
                    double v = stat.Value.GetDouble();
                    if (v > 1.05) up = stat.Name;
                    else if (v < 0.95) down = stat.Name;
                }
            }
            result[prop.Name] = new NatureModifiers(up, down);
        }
        return result;
    }
}
