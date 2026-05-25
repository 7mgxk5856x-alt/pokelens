using System.Text.Json.Serialization;

namespace PokelensCore.Models;

/// <summary>1 技分のエントリ（party.json の moves[] 要素と 1:1 対応）。</summary>
public sealed class MoveEntryModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}
