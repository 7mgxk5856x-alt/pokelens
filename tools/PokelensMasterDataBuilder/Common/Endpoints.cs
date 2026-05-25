namespace PokelensMasterDataBuilder.Common;

/// <summary>外部から取得する HTTP エンドポイント。アクセス先（サービス）ごとに入れ子クラスで一元管理する。</summary>
internal static class Endpoints
{
    /// <summary>Pokémon Showdown のデータ JS。</summary>
    internal static class Showdown
    {
        private const string DataBase = "https://play.pokemonshowdown.com/data";
        internal const string Pokedex = $"{DataBase}/pokedex.js";
        internal const string Moves = $"{DataBase}/moves.js";
        internal const string Items = $"{DataBase}/items.js";
        internal const string Abilities = $"{DataBase}/abilities.js";
    }

    /// <summary>PokéAPI（ID / slug を埋めて URL を生成する）。</summary>
    internal static class PokeApi
    {
        private const string Base = "https://pokeapi.co/api/v2";
        internal static string Move(int id) => $"{Base}/move/{id}/";
        internal static string Ability(int id) => $"{Base}/ability/{id}/";
        internal static string Item(string slug) => $"{Base}/item/{slug}/";
        internal static string PokemonSpecies(int num) => $"{Base}/pokemon-species/{num}/";
        internal static string PokemonForm(string slug) => $"{Base}/pokemon-form/{slug}/";
    }
}
