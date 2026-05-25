namespace PokelensCore;

/// <summary>tools/ 配下の各 C# プロジェクト（PokelensMasterDataBuilder / PokelensPartyEditor）が共有するファイル・ディレクトリのパス。</summary>
/// <remarks>
/// 既定では <see cref="Directory.GetCurrentDirectory"/> の起動時 CWD をリポジトリルートとする。
/// テスト等は <see cref="OverrideRepoRoot"/> で IDisposable scope を取り、その間だけ別の root に
/// 切り替えられる。override は <see cref="AsyncLocal{T}"/> ベースなので並列テストでも互いに干渉しない。
/// パイプライン固有の Cache/Patch パスは <c>PokelensMasterDataBuilder.Common.DataPaths</c> 側に残し、
/// 本クラスでは「複数プロジェクトで共有する RepoRoot と data/ パス」のみを管理する。
/// </remarks>
public static class DataPaths
{
    /// <summary>scope ごとの RepoRoot 上書き値。null なら CWD を使う。</summary>
    private static readonly AsyncLocal<string?> RepoRootOverride = new();

    /// <summary>リポジトリルート。<see cref="OverrideRepoRoot"/> で上書きされていればその値、なければ起動時 CWD。</summary>
    public static string RepoRoot => RepoRootOverride.Value ?? Directory.GetCurrentDirectory();

    /// <summary>RepoRoot を一時的に上書きする scope を返す。Dispose で前の値に復元する。</summary>
    /// <remarks>テスト隔離専用。本番コードは呼ばない。AsyncLocal なので並列テスト間で干渉しない。</remarks>
    /// <param name="root">上書きするリポジトリルート（絶対パス想定）。</param>
    /// <returns>Dispose で復元する scope。</returns>
    public static IDisposable OverrideRepoRoot(string root)
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

    /// <summary>data/ 配下のマスターデータファイル群。</summary>
    /// <remarks>各メソッドは <see cref="RepoRoot"/> 直下の data/ 配下の絶対パスを返す。</remarks>
    public static class Master
    {
        /// <summary>data/ ディレクトリ。</summary>
        public static string Dir => Path.Combine(RepoRoot, "data");

        public static string Pokedex() => Path.Combine(Dir, "pokedex.json");
        public static string Moves() => Path.Combine(Dir, "moves.json");
        public static string Items() => Path.Combine(Dir, "items.json");
        public static string Abilities() => Path.Combine(Dir, "abilities.json");
        public static string Natures() => Path.Combine(Dir, "natures.json");
        public static string Types() => Path.Combine(Dir, "types.json");
        public static string MoveCategories() => Path.Combine(Dir, "move-categories.json");
        public static string Party() => Path.Combine(Dir, "party.json");
    }
}
