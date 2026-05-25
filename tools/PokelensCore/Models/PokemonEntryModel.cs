using System.Text.Json.Serialization;

namespace PokelensCore.Models;

/// <summary>1 匹分のポケモンエントリ（party.json の party[] 要素と 1:1 対応）。</summary>
/// <remarks>
/// 必須フィールド（species / nature / abilityPoints / moves）が欠落した JSON はロード時に弾く（PartyFileService 側）。
/// 本型はデシリアライズ後の状態を保持するだけで、必須チェックは外側の責務。
/// </remarks>
public sealed class PokemonEntryModel
{
    [JsonPropertyName("species")] public string Species { get; set; } = string.Empty;
    [JsonPropertyName("ability")] public string? Ability { get; set; }
    [JsonPropertyName("item")] public string? Item { get; set; }
    [JsonPropertyName("nature")] public string Nature { get; set; } = string.Empty;
    [JsonPropertyName("abilityPoints")] public AbilityPointsModel AbilityPoints { get; set; } = new();
    [JsonPropertyName("moves")] public List<MoveEntryModel> Moves { get; set; } = new();
}
