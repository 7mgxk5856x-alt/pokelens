using PokelensTools.Common;
using PokelensTools.Fetchers;
using PokelensTools.Models;
using PokelensTools.Pipeline;

// リポジトリルートを特定する。CLAUDE.md の指示通り
// "dotnet run --project tools/PokelensTools" でリポジトリルートから実行した場合、
// カレントディレクトリがそのままリポジトリルートになる。
if (!Directory.Exists(DataPaths.ToolsDir))
{
    throw new DirectoryNotFoundException(
        $"リポジトリルートが特定できません。CLAUDE.md の指示通り、リポジトリルートから " +
        $"'dotnet run --project tools/PokelensTools' で実行してください。(CWD: {DataPaths.RepoRoot})");
}

// HttpClient を両 fetcher に注入して共有する（各 fetcher が独自の static インスタンスを
// 持たないようにするため）。ライフタイムは Program.cs に束ね、終了時に dispose する。
using var http = new HttpClient();
var showdownFetcher = new ShowdownFetcher(http);
var pokeApiFetcher = new PokeAPIFetcher(http);

// Step 1: Showdown データは常に取得する
Console.WriteLine("[Step 1] Fetching Showdown data...");
await showdownFetcher.FetchAllAsync();
Console.WriteLine("  Done.");

// 差分実行の判定用に現在のチェックサムを計算する
var currentChecksums = new ChecksumSet(
    ShowdownPokedex: IncrementalRunner.ComputeHash(DataPaths.Cache.ShowdownPokedex()),
    ShowdownMoves: IncrementalRunner.ComputeHash(DataPaths.Cache.ShowdownMoves()),
    ShowdownItems: IncrementalRunner.ComputeHash(DataPaths.Cache.ShowdownItems()),
    ShowdownAbilities: IncrementalRunner.ComputeHash(DataPaths.Cache.ShowdownAbilities()),
    PokeApiTranslations: IncrementalRunner.ComputeHash(DataPaths.Cache.PokeApiTranslations()),
    ChampionsPatch: IncrementalRunner.ComputeHash(DataPaths.Patch.Champions()),
    MovesPowerPatch: IncrementalRunner.ComputeHash(DataPaths.Patch.MovesPower()),
    ItemsModifiers: IncrementalRunner.ComputeHash(DataPaths.Patch.ItemsModifiers()),
    AbilitiesModifiers: IncrementalRunner.ComputeHash(DataPaths.Patch.AbilitiesModifiers()),
    PokemonNamePatch: IncrementalRunner.ComputeHash(DataPaths.Patch.PokemonNamePatch()),
    ItemNamePatch: IncrementalRunner.ComputeHash(DataPaths.Patch.ItemNamePatch()));

ChecksumSet? previousChecksums = IncrementalRunner.LoadChecksums(DataPaths.Cache.Checksums());
IncrementalRunner.Steps steps = IncrementalRunner.DetermineSteps(previousChecksums, currentChecksums);

if (!steps.NeedsStep2 && !steps.NeedsStep3 && !steps.NeedsStep4)
{
    Console.WriteLine("No changes detected. data/ is up to date.");
    return;
}

// Step 2: PokéAPI の翻訳を取得する（必要な場合のみ）
if (steps.NeedsStep2)
{
    Console.WriteLine("[Step 2] Fetching PokéAPI translations...");
    await pokeApiFetcher.FetchTranslationsAsync();
    Console.WriteLine("  Done.");

    // 取得後に PokéAPI 翻訳のハッシュを更新する
    currentChecksums = currentChecksums with
    {
        PokeApiTranslations = IncrementalRunner.ComputeHash(DataPaths.Cache.PokeApiTranslations()),
    };
}

// Step 3: champions-patch.json を適用する（必要な場合のみ）
if (steps.NeedsStep3)
{
    Console.WriteLine("[Step 3] Applying champions-patch.json...");
    PatchApplicator.Apply(
        DataPaths.Cache.ShowdownPokedex(),
        DataPaths.Cache.ShowdownMoves(),
        DataPaths.Patch.Champions());
    Console.WriteLine("  Done.");
}

// Step 4: 最終成果物を生成する
if (steps.NeedsStep4)
{
    Console.WriteLine("[Step 4] Generating data/*.json...");
    MergeConverter.Convert(
        DataPaths.Cache.ShowdownPokedex(),
        DataPaths.Cache.ShowdownMoves(),
        DataPaths.Cache.ShowdownItems(),
        DataPaths.Cache.ShowdownAbilities(),
        DataPaths.Cache.PokeApiTranslations(),
        DataPaths.Patch.MovesPower(),
        DataPaths.Patch.ItemsModifiers(),
        DataPaths.Patch.AbilitiesModifiers(),
        DataPaths.Patch.PokemonNamePatch(),
        DataPaths.Patch.ItemNamePatch(),
        DataPaths.Master.Dir);
    Console.WriteLine("  Done.");
}

// チェックサムを更新する
// Step2〜Step4 が例外で終了した場合は SaveChecksums に到達しないため、
// 次回起動で同ステップから再実行される（部分失敗の回復性を担保するための意図的設計）。
IncrementalRunner.SaveChecksums(currentChecksums, DataPaths.Cache.Checksums());
Console.WriteLine("Completed successfully.");
