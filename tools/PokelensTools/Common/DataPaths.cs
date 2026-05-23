namespace PokelensTools.Common;

/// <summary>tools/ パイプラインが扱うファイルとディレクトリのパス。</summary>
/// <remarks>
/// リポジトリルートは <see cref="Directory.GetCurrentDirectory"/> で起動時に 1 回キャプチャし、
/// 静的フィールドに保持する。各ファイル種別（Cache / Master / Patch）に対して 2 つのオーバーロードを提供する:
/// <list type="bullet">
/// <item>引数なし — 本番固定パス。Program.cs などの wiring 層、および HTTP fetcher のように
/// 実テストで dir が変動しない library 関数からも使う（fake parameterization を避けるため）。</item>
/// <item>引数あり（dir 指定）— **実テストで dir を差し替えて呼ばれる** library 関数（例: MergeConverter、
/// PatchApplicator — PipelineIntegrationTests が temp dir を渡す）から使う。テスト隔離のため、これらの関数
/// では内部で必ずこちらを使うこと（引数なし版を使うと temp dir を無視して本番側へ書き込み隔離が壊れる）。</item>
/// </list>
/// 判断基準は「将来テストするかも」ではなく「**今、実テストで引数が変動しているか**」。
/// 詳細は development-guidelines.md の同名節を参照。
/// </remarks>
internal static class DataPaths
{
    /// <summary>リポジトリルート（起動時の CWD を 1 回だけキャプチャ）。</summary>
    internal static readonly string RepoRoot = Directory.GetCurrentDirectory();

    /// <summary>tools/PokelensTools/ ディレクトリ。リポジトリルート検証にも使う。</summary>
    internal static readonly string ToolsDir = Path.Combine(RepoRoot, "tools", "PokelensTools");

    /// <summary>cache/ 配下の中間キャッシュファイル。</summary>
    /// <remarks>引数なし版は本番 cache/ 配下、引数あり版は呼び出し元から受け取った任意の dir 配下のパスを返す。
    /// 現状 Fetcher（HTTP 取得）は実テストで dir が変動しないため引数なし版を直接使っている。
    /// 将来、テストで dir を差し替えて library 関数を呼ぶケースが生じたら、その関数では引数あり版を使うこと。</remarks>
    internal static class Cache
    {
        /// <summary>cache/ ディレクトリ（本番）。</summary>
        internal static readonly string Dir = Path.Combine(RepoRoot, "cache");

        internal static string ShowdownPokedex() => ShowdownPokedex(Dir);
        internal static string ShowdownPokedex(string cacheDir) => Path.Combine(cacheDir, "showdown-pokedex.json");

        internal static string ShowdownMoves() => ShowdownMoves(Dir);
        internal static string ShowdownMoves(string cacheDir) => Path.Combine(cacheDir, "showdown-moves.json");

        internal static string ShowdownItems() => ShowdownItems(Dir);
        internal static string ShowdownItems(string cacheDir) => Path.Combine(cacheDir, "showdown-items.json");

        internal static string ShowdownAbilities() => ShowdownAbilities(Dir);
        internal static string ShowdownAbilities(string cacheDir) => Path.Combine(cacheDir, "showdown-abilities.json");

        internal static string PokeApiTranslations() => PokeApiTranslations(Dir);
        internal static string PokeApiTranslations(string cacheDir) => Path.Combine(cacheDir, "pokeapi-translations.json");

        internal static string Checksums() => Checksums(Dir);
        internal static string Checksums(string cacheDir) => Path.Combine(cacheDir, "checksums.json");
    }

    /// <summary>data/ 配下のフロントエンド向けマスタ出力 JSON。</summary>
    /// <remarks>引数なし版は本番 data/ 配下、引数あり版は呼び出し元から受け取った任意の dir 配下のパスを返す。
    /// MergeConverter は PipelineIntegrationTests で temp dataDir を受けるため、内部で必ず引数あり版を使う。</remarks>
    internal static class Master
    {
        /// <summary>data/ ディレクトリ（本番）。</summary>
        internal static readonly string Dir = Path.Combine(RepoRoot, "data");

        internal static string Pokedex() => Pokedex(Dir);
        internal static string Pokedex(string dataDir) => Path.Combine(dataDir, "pokedex.json");

        internal static string Moves() => Moves(Dir);
        internal static string Moves(string dataDir) => Path.Combine(dataDir, "moves.json");

        internal static string Items() => Items(Dir);
        internal static string Items(string dataDir) => Path.Combine(dataDir, "items.json");

        internal static string Abilities() => Abilities(Dir);
        internal static string Abilities(string dataDir) => Path.Combine(dataDir, "abilities.json");
    }

    /// <summary>tools/PokelensTools/Patches/ 配下の手動パッチファイル。</summary>
    /// <remarks>引数なし版は本番 Patches/ 配下、引数あり版は呼び出し元から受け取った任意の dir 配下のパスを返す。
    /// 現状 PatchApplicator は呼び出し元（Program.cs）から完全パスを受け取る設計のため DataPaths 自体を内部参照しない。
    /// 将来 library 関数を追加する場合、実テストで dir が変動するなら引数あり版を、固定なら引数なし版を内部で使う。</remarks>
    internal static class Patch
    {
        /// <summary>Patches/ ディレクトリ（本番）。</summary>
        internal static readonly string Dir = Path.Combine(ToolsDir, "Patches");

        internal static string Champions() => Champions(Dir);
        internal static string Champions(string patchesDir) => Path.Combine(patchesDir, "champions-patch.json");

        internal static string MovesPower() => MovesPower(Dir);
        internal static string MovesPower(string patchesDir) => Path.Combine(patchesDir, "moves-power-patch.json");

        internal static string ItemsModifiers() => ItemsModifiers(Dir);
        internal static string ItemsModifiers(string patchesDir) => Path.Combine(patchesDir, "items-modifiers.json");

        internal static string AbilitiesModifiers() => AbilitiesModifiers(Dir);
        internal static string AbilitiesModifiers(string patchesDir) => Path.Combine(patchesDir, "abilities-modifiers.json");

        internal static string PokemonNamePatch() => PokemonNamePatch(Dir);
        internal static string PokemonNamePatch(string patchesDir) => Path.Combine(patchesDir, "pokemon-name-patch.json");

        internal static string ItemNamePatch() => ItemNamePatch(Dir);
        internal static string ItemNamePatch(string patchesDir) => Path.Combine(patchesDir, "item-name-patch.json");
    }
}
