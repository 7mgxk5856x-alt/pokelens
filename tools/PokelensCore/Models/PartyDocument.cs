using System.Text.Json.Serialization;

namespace PokelensCore.Models;

/// <summary>data/party.json のルート構造。<c>party: PokemonEntryModel[]</c> のラッパー。</summary>
public sealed class PartyDocument
{
    [JsonPropertyName("party")] public List<PokemonEntryModel> Party { get; set; } = new();
}
