using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokelensCore.Models;
using PokelensPartyEditor.Services;

namespace PokelensPartyEditor.ViewModels;

/// <summary>1 匹分のポケモンエントリの ViewModel。</summary>
/// <remarks>
/// 入力プロパティ・サジェスト候補・能力ポイント補助コマンド・バリデーションを担当する。
/// 値が変化したら <see cref="ObservableObject.PropertyChanged"/> が発火し、<see cref="MainWindowViewModel"/> が
/// ダーティフラグへ反映する。
/// </remarks>
public sealed partial class PokemonEntryViewModel : ObservableObject
{
    private const int AbilityPointMin = 0;
    private const int AbilityPointMax = 32;

    private readonly ISuggestService _suggest;
    private readonly MasterDataSnapshot _master;

    public PokemonEntryViewModel(ISuggestService suggest, MasterDataSnapshot master)
    {
        _suggest = suggest;
        _master = master;
        NatureChoices = master.NatureNames
            .Select(n => new NatureChoice(n, FormatNatureLabel(n, master.NatureModifiers)))
            .ToList();
        Moves = new List<MoveSlotViewModel>
        {
            new(this, 0), new(this, 1), new(this, 2), new(this, 3),
        };
    }

    [ObservableProperty] private string _species = string.Empty;
    [ObservableProperty] private string _ability = string.Empty;
    [ObservableProperty] private string _item = string.Empty;
    [ObservableProperty] private string? _nature;

    [ObservableProperty] private string _hp = "0";
    [ObservableProperty] private string _atk = "0";
    [ObservableProperty] private string _def = "0";
    [ObservableProperty] private string _spa = "0";
    [ObservableProperty] private string _spd = "0";
    [ObservableProperty] private string _spe = "0";

    public IReadOnlyList<NatureChoice> NatureChoices { get; }
    public IReadOnlyList<MoveSlotViewModel> Moves { get; }

    private static string FormatNatureLabel(string name, IReadOnlyDictionary<string, NatureModifiers> mods)
    {
        if (!mods.TryGetValue(name, out var m) || (m.UpStat is null && m.DownStat is null))
        {
            return $"{name} (補正なし)";
        }
        string up = StatToSymbol(m.UpStat);
        string down = StatToSymbol(m.DownStat);
        return $"{name} ({up}↑/{down}↓)";
    }

    private static string StatToSymbol(string? stat) => stat switch
    {
        "atk" => "A",
        "def" => "B",
        "spa" => "C",
        "spd" => "D",
        "spe" => "S",
        _ => "-",
    };

    /// <summary>能力ポイントを 0 にする（補助ボタン「0」）。</summary>
    public void ResetPoint(string field) => SetPoint(field, "0");

    /// <summary>能力ポイントを 32 にする（補助ボタン「最大」）。</summary>
    public void MaxPoint(string field) => SetPoint(field, AbilityPointMax.ToString());

    /// <summary>能力ポイントを +1 する（補助ボタン「+」）。0〜32 整数のみ対象、範囲外は変更なし。</summary>
    public void IncrementPoint(string field) => AdjustPoint(field, +1);

    /// <summary>能力ポイントを -1 する（補助ボタン「-」）。0〜32 整数のみ対象、範囲外は変更なし。</summary>
    public void DecrementPoint(string field) => AdjustPoint(field, -1);

    private void AdjustPoint(string field, int delta)
    {
        string current = GetPoint(field);
        if (!int.TryParse(current, out int value)) return;
        if (value < AbilityPointMin || value > AbilityPointMax) return;
        int next = value + delta;
        if (next < AbilityPointMin || next > AbilityPointMax) return;
        SetPoint(field, next.ToString());
    }

    /// <summary>能力ポイント欄を field 名で取得する。</summary>
    public string GetPoint(string field) => field switch
    {
        "hp" => Hp,
        "atk" => Atk,
        "def" => Def,
        "spa" => Spa,
        "spd" => Spd,
        "spe" => Spe,
        _ => throw new ArgumentException($"Unknown ability point field: {field}"),
    };

    /// <summary>能力ポイント欄を field 名で設定する。</summary>
    public void SetPoint(string field, string value)
    {
        switch (field)
        {
            case "hp": Hp = value; break;
            case "atk": Atk = value; break;
            case "def": Def = value; break;
            case "spa": Spa = value; break;
            case "spd": Spd = value; break;
            case "spe": Spe = value; break;
            default: throw new ArgumentException($"Unknown ability point field: {field}");
        }
    }

    /// <summary>サジェスト候補を 4 種類のフィールド分岐で返す。</summary>
    public IReadOnlyList<string> SuggestFor(string field, string query) => field switch
    {
        "species" => _suggest.SuggestPokemon(query),
        "ability" => _suggest.SuggestAbility(query),
        "item" => _suggest.SuggestItem(query),
        "move" => _suggest.SuggestMove(query),
        _ => throw new ArgumentException($"Unknown suggest field: {field}"),
    };

    /// <summary>入力内容を <see cref="PokemonEntryModel"/> に変換する（セーブ前のシリアライズ用）。</summary>
    public PokemonEntryModel ToModel() => new()
    {
        Species = Species,
        Ability = string.IsNullOrEmpty(Ability) ? null : Ability,
        Item = string.IsNullOrEmpty(Item) ? null : Item,
        Nature = Nature ?? string.Empty,
        AbilityPoints = new AbilityPointsModel
        {
            Hp = TryParseOrZero(Hp),
            Atk = TryParseOrZero(Atk),
            Def = TryParseOrZero(Def),
            Spa = TryParseOrZero(Spa),
            Spd = TryParseOrZero(Spd),
            Spe = TryParseOrZero(Spe),
        },
        Moves = Moves
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .Select(m => new MoveEntryModel { Name = m.Name })
            .ToList(),
    };

    /// <summary>モデルから入力欄に反映する（ロード時）。</summary>
    public void LoadFrom(PokemonEntryModel model)
    {
        Species = model.Species;
        Ability = model.Ability ?? string.Empty;
        Item = model.Item ?? string.Empty;
        Nature = string.IsNullOrEmpty(model.Nature) ? null : model.Nature;
        Hp = model.AbilityPoints.Hp.ToString();
        Atk = model.AbilityPoints.Atk.ToString();
        Def = model.AbilityPoints.Def.ToString();
        Spa = model.AbilityPoints.Spa.ToString();
        Spd = model.AbilityPoints.Spd.ToString();
        Spe = model.AbilityPoints.Spe.ToString();
        for (int i = 0; i < Moves.Count; i++)
        {
            Moves[i].Name = i < model.Moves.Count ? model.Moves[i].Name : string.Empty;
        }
    }

    /// <summary>初期表示（全欄空欄・能力ポイント 0）。</summary>
    public void Reset()
    {
        Species = string.Empty;
        Ability = string.Empty;
        Item = string.Empty;
        Nature = null;
        Hp = Atk = Def = Spa = Spd = Spe = "0";
        foreach (var m in Moves) m.Name = string.Empty;
    }

    /// <summary>このエントリを単独でバリデーションし、エラーを <paramref name="errors"/> に追加する。</summary>
    /// <param name="slotIndex">エントリ位置（0-5）。</param>
    /// <param name="errors">エラー累積先。</param>
    public void Validate(int slotIndex, List<ValidationError> errors)
    {
        // 完全空欄スロットはスキップ（未入力スロットはセーブ時に「未入力確認」ダイアログ側で扱う）
        if (IsCompletelyEmpty()) return;

        if (string.IsNullOrEmpty(Species))
        {
            errors.Add(new ValidationError(slotIndex, "species", "ポケモン名が未入力です"));
        }
        else if (!_master.PokemonNames.Contains(Species))
        {
            errors.Add(new ValidationError(slotIndex, "species", $"ポケモン名 '{Species}' はマスターデータに存在しません"));
        }

        if (!string.IsNullOrEmpty(Ability) && !_master.AbilityNames.Contains(Ability))
        {
            errors.Add(new ValidationError(slotIndex, "ability", $"特性 '{Ability}' はマスターデータに存在しません"));
        }

        if (!string.IsNullOrEmpty(Item) && !_master.ItemNames.Contains(Item))
        {
            errors.Add(new ValidationError(slotIndex, "item", $"アイテム '{Item}' はマスターデータに存在しません"));
        }

        if (!string.IsNullOrEmpty(Nature) && !_master.NatureNames.Contains(Nature!))
        {
            errors.Add(new ValidationError(slotIndex, "nature", $"性格 '{Nature}' はマスターデータに存在しません"));
        }

        // 能力ポイント: 各 0〜32 整数 + 合計 0〜66
        int total = 0;
        foreach (var field in new[] { "hp", "atk", "def", "spa", "spd", "spe" })
        {
            string raw = GetPoint(field);
            if (!int.TryParse(raw, out int v) || v < AbilityPointMin || v > AbilityPointMax)
            {
                errors.Add(new ValidationError(slotIndex, "abilityPoints",
                    $"能力ポイント '{field}' が 0〜32 の整数ではありません (現在値: '{raw}')"));
                continue;
            }
            total += v;
        }
        if (total > 66)
        {
            errors.Add(new ValidationError(slotIndex, "abilityPointsTotal",
                $"能力ポイント合計が 66 を超えています (現在値: {total})"));
        }

        // 技: 空欄技はマスター照合スキップ、入力ありはマスター存在チェック
        for (int i = 0; i < Moves.Count; i++)
        {
            string name = Moves[i].Name;
            if (string.IsNullOrEmpty(name)) continue;
            if (!_master.MoveNames.Contains(name))
            {
                errors.Add(new ValidationError(slotIndex, $"move{i}",
                    $"技 '{name}' はマスターデータに存在しません"));
            }
        }
    }

    /// <summary>このエントリが完全に空欄（モデル化したら省略可能）かどうか。</summary>
    public bool IsCompletelyEmpty()
    {
        if (!string.IsNullOrEmpty(Species)) return false;
        if (!string.IsNullOrEmpty(Ability)) return false;
        if (!string.IsNullOrEmpty(Item)) return false;
        if (!string.IsNullOrEmpty(Nature)) return false;
        if (Hp != "0" || Atk != "0" || Def != "0" || Spa != "0" || Spd != "0" || Spe != "0") return false;
        if (Moves.Any(m => !string.IsNullOrEmpty(m.Name))) return false;
        return true;
    }

    /// <summary>このエントリに未入力項目（必須欄空欄 or 空欄技含む）があるかを判定する。</summary>
    /// <remarks>セーブ前の確認ダイアログ判定で使用。完全空欄スロットは未入力扱いしない。</remarks>
    public bool HasUnfilledFields()
    {
        if (IsCompletelyEmpty()) return false;
        if (string.IsNullOrEmpty(Species)) return true;
        if (string.IsNullOrEmpty(Ability)) return true;
        if (string.IsNullOrEmpty(Item)) return true;
        if (string.IsNullOrEmpty(Nature)) return true;
        if (Moves.Any(m => string.IsNullOrEmpty(m.Name))) return true;
        return false;
    }

    private static int TryParseOrZero(string s) => int.TryParse(s, out int v) ? v : 0;
}

/// <summary>性格プルダウン用の項目。</summary>
/// <param name="Key">性格名（party.json への保存値）。</param>
/// <param name="DisplayLabel">表示ラベル（「いじっぱり (A↑/C↓)」）。</param>
public sealed record NatureChoice(string Key, string DisplayLabel);

/// <summary>技 1 件分の ViewModel（4 つで 1 ポケモン）。</summary>
public sealed partial class MoveSlotViewModel : ObservableObject
{
    private readonly PokemonEntryViewModel _parent;
    public int Index { get; }

    public MoveSlotViewModel(PokemonEntryViewModel parent, int index)
    {
        _parent = parent;
        Index = index;
    }

    [ObservableProperty] private string _name = string.Empty;

    public IReadOnlyList<string> Suggest(string query) => _parent.SuggestFor("move", query);
}
