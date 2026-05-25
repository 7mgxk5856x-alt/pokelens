using System.Text.Json.Serialization;

namespace PokelensCore.Models;

/// <summary>能力ポイント 6 値（HP/Atk/Def/SpA/SpD/Spe）。各値は 0〜32 の整数（Pokémon Champions 仕様）。</summary>
/// <remarks>
/// 範囲外の値はバリデーション（PokelensPartyEditor 側）で弾く。本型は値の保持のみを担当し、範囲チェックは外側で行う。
/// 合計値の上限（66）も外側でチェックする。
/// </remarks>
public sealed class AbilityPointsModel
{
    [JsonPropertyName("hp")] public int Hp { get; set; }
    [JsonPropertyName("atk")] public int Atk { get; set; }
    [JsonPropertyName("def")] public int Def { get; set; }
    [JsonPropertyName("spa")] public int Spa { get; set; }
    [JsonPropertyName("spd")] public int Spd { get; set; }
    [JsonPropertyName("spe")] public int Spe { get; set; }

    public int Total => Hp + Atk + Def + Spa + Spd + Spe;
}
