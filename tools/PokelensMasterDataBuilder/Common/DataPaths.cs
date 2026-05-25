namespace PokelensMasterDataBuilder.Common;

/// <summary>tools/ パイプラインが扱うファイルとディレクトリのパス。</summary>
/// <remarks>
/// 既定では <see cref="Directory.GetCurrentDirectory"/> の起動時 CWD をリポジトリルートとする。
/// テスト等は <see cref="OverrideRepoRoot"/> で IDisposable scope を取り、その間だけ別の root に
/// 切り替えられる。override は <see cref="AsyncLocal{T}"/> ベースなので並列テストでも互いに干渉しない。
/// Library 関数（Fetcher / Pipeline / IncrementalRunner 等）は path 引数を取らず、内部で <c>DataPaths</c>
/// のプロパティ・メソッドを直接参照する。テストは <see cref="OverrideRepoRoot"/> 経由で挙動を redirect する。
/// </remarks>
internal static class DataPaths
{
    /// <summary>scope ごとの RepoRoot 上書き値。null なら CWD を使う。</summary>
    private static readonly AsyncLocal<string?> RepoRootOverride = new();

    /// <summary>リポジトリルート。<see cref="OverrideRepoRoot"/> で上書きされていればその値、なければ起動時 CWD。</summary>
    internal static string RepoRoot => RepoRootOverride.Value ?? Directory.GetCurrentDirectory();

    /// <summary>tools/PokelensMasterDataBuilder/ ディレクトリ。リポジトリルート検証にも使う。</summary>
    internal static string ToolsDir => Path.Combine(RepoRoot, "tools", "PokelensMasterDataBuilder");

    /// <summary>RepoRoot を一時的に上書きする scope を返す。Dispose で前の値に復元する。</summary>
    /// <remarks>テスト隔離専用。本番コードは呼ばない。AsyncLocal なので並列テスト間で干渉しない。</remarks>
    /// <param name="root">上書きするリポジトリルート（絶対パス想定）。</param>
    /// <returns>Dispose で復元する scope。</returns>
    internal static IDisposable OverrideRepoRoot(string root)
    {
        string? previous = RepoRootOverride.Value;
        RepoRootOverride.Value = root;
        return new RestoreScope(() => RepoRootOverride.Value = previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly Action _onDispose;
        internal RestoreScope(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }

    /// <summary>cache/ 配下の中間キャッシュファイル。</summary>
    /// <remarks>各メソッドは <see cref="RepoRoot"/> 直下の cache/ 配下の絶対パスを返す。</remarks>
    internal static class Cache
    {
        /// <summary>cache/ ディレクトリ。</summary>
        internal static string Dir => Path.Combine(RepoRoot, "cache");

        internal static string ShowdownPokedex() => Path.Combine(Dir, "showdown-pokedex.json");
        internal static string ShowdownMoves() => Path.Combine(Dir, "showdown-moves.json");
        internal static string ShowdownItems() => Path.Combine(Dir, "showdown-items.json");
        internal static string ShowdownAbilities() => Path.Combine(Dir, "showdown-abilities.json");
        internal static string PokeApiTranslations() => Path.Combine(Dir, "pokeapi-translations.json");
        internal static string Checksums() => Path.Combine(Dir, "checksums.json");
    }

    /// <summary>data/ 配下のフロントエンド向けマスタ出力 JSON。</summary>
    /// <remarks>各メソッドは <see cref="RepoRoot"/> 直下の data/ 配下の絶対パスを返す。</remarks>
    internal static class Master
    {
        /// <summary>data/ ディレクトリ。</summary>
        internal static string Dir => Path.Combine(RepoRoot, "data");

        internal static string Pokedex() => Path.Combine(Dir, "pokedex.json");
        internal static string Moves() => Path.Combine(Dir, "moves.json");
        internal static string Items() => Path.Combine(Dir, "items.json");
        internal static string Abilities() => Path.Combine(Dir, "abilities.json");
    }

    /// <summary>tools/PokelensMasterDataBuilder/Patches/ 配下の手動パッチファイル。</summary>
    /// <remarks>各メソッドは <see cref="ToolsDir"/> 配下の Patches/ の絶対パスを返す。</remarks>
    internal static class Patch
    {
        /// <summary>Patches/ ディレクトリ。</summary>
        internal static string Dir => Path.Combine(ToolsDir, "Patches");

        internal static string Champions() => Path.Combine(Dir, "champions-patch.json");
        internal static string MovesPower() => Path.Combine(Dir, "moves-power-patch.json");
        internal static string ItemsModifiers() => Path.Combine(Dir, "items-modifiers.json");
        internal static string AbilitiesModifiers() => Path.Combine(Dir, "abilities-modifiers.json");
        internal static string PokemonNamePatch() => Path.Combine(Dir, "pokemon-name-patch.json");
        internal static string ItemNamePatch() => Path.Combine(Dir, "item-name-patch.json");
    }
}
