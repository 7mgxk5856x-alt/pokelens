using PokelensTools;

// リポジトリルートを特定する。CLAUDE.md の指示通り
// "dotnet run --project tools/PokelensTools" でリポジトリルートから実行した場合、
// カレントディレクトリがそのままリポジトリルートになる。
string repoRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(repoRoot, "tools", "PokelensTools")))
{
    throw new DirectoryNotFoundException(
        $"リポジトリルートが特定できません。CLAUDE.md の指示通り、リポジトリルートから " +
        $"'dotnet run --project tools/PokelensTools' で実行してください。(CWD: {repoRoot})");
}
string cacheDir = Path.Combine(repoRoot, "cache");
string dataDir = Path.Combine(repoRoot, "data");
string toolsDir = Path.Combine(repoRoot, "tools", "PokelensTools");

string checksumsPath       = Path.Combine(cacheDir, "checksums.json");
string pokedexCachePath    = Path.Combine(cacheDir, CacheFileName.ShowdownPokedex);
string movesCachePath      = Path.Combine(cacheDir, CacheFileName.ShowdownMoves);
string itemsCachePath      = Path.Combine(cacheDir, CacheFileName.ShowdownItems);
string abilitiesCachePath  = Path.Combine(cacheDir, CacheFileName.ShowdownAbilities);
string translationsPath    = Path.Combine(cacheDir, CacheFileName.PokeApiTranslations);

string patchesDir = Path.Combine(toolsDir, "Patches");
string championsPatchPath    = Path.Combine(patchesDir, "champions-patch.json");
string movesPowerPatchPath   = Path.Combine(patchesDir, "moves-power-patch.json");
string itemsModifiersPath    = Path.Combine(patchesDir, "items-modifiers.json");
string abilitiesModifiersPath = Path.Combine(patchesDir, "abilities-modifiers.json");
string pokemonNamePatchPath  = Path.Combine(patchesDir, "pokemon-name-patch.json");
string itemNamePatchPath     = Path.Combine(patchesDir, "item-name-patch.json");

// HttpClient を両 fetcher に注入して共有する（各 fetcher が独自の static インスタンスを
// 持たないようにするため）。ライフタイムは Program.cs に束ね、終了時に dispose する。
using var http = new HttpClient();
var showdownFetcher = new ShowdownFetcher(http);
var pokeApiFetcher = new PokeAPIFetcher(http);

// Step 1: Showdown データは常に取得する
Console.WriteLine("[Step 1] Fetching Showdown data...");
await showdownFetcher.FetchAllAsync(cacheDir);
Console.WriteLine("  Done.");

// 差分実行の判定用に現在のチェックサムを計算する
var currentChecksums = new ChecksumSet(
    ShowdownPokedex: IncrementalRunner.ComputeHash(pokedexCachePath),
    ShowdownMoves: IncrementalRunner.ComputeHash(movesCachePath),
    ShowdownItems: IncrementalRunner.ComputeHash(itemsCachePath),
    ShowdownAbilities: IncrementalRunner.ComputeHash(abilitiesCachePath),
    PokeApiTranslations: IncrementalRunner.ComputeHash(translationsPath),
    ChampionsPatch: IncrementalRunner.ComputeHash(championsPatchPath),
    MovesPowerPatch: IncrementalRunner.ComputeHash(movesPowerPatchPath),
    ItemsModifiers: IncrementalRunner.ComputeHash(itemsModifiersPath),
    AbilitiesModifiers: IncrementalRunner.ComputeHash(abilitiesModifiersPath),
    PokemonNamePatch: IncrementalRunner.ComputeHash(pokemonNamePatchPath),
    ItemNamePatch: IncrementalRunner.ComputeHash(itemNamePatchPath));

ChecksumSet? previousChecksums = IncrementalRunner.LoadChecksums(checksumsPath);
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
    await pokeApiFetcher.FetchTranslationsAsync(
        cacheDir,
        pokedexCachePath,
        movesCachePath,
        itemsCachePath,
        abilitiesCachePath);
    Console.WriteLine("  Done.");

    // 取得後に PokéAPI 翻訳のハッシュを更新する
    currentChecksums = currentChecksums with
    {
        PokeApiTranslations = IncrementalRunner.ComputeHash(translationsPath),
    };
}

// Step 3: champions-patch.json を適用する（必要な場合のみ）
if (steps.NeedsStep3)
{
    Console.WriteLine("[Step 3] Applying champions-patch.json...");
    PatchApplicator.Apply(pokedexCachePath, movesCachePath, championsPatchPath);
    Console.WriteLine("  Done.");
}

// Step 4: 最終成果物を生成する
if (steps.NeedsStep4)
{
    Console.WriteLine("[Step 4] Generating data/*.json...");
    MergeConverter.Convert(
        pokedexCachePath,
        movesCachePath,
        itemsCachePath,
        abilitiesCachePath,
        translationsPath,
        movesPowerPatchPath,
        itemsModifiersPath,
        abilitiesModifiersPath,
        pokemonNamePatchPath,
        itemNamePatchPath,
        dataDir);
    Console.WriteLine("  Done.");
}

// チェックサムを更新する
// Step2〜Step4 が例外で終了した場合は SaveChecksums に到達しないため、
// 次回起動で同ステップから再実行される（部分失敗の回復性を担保するための意図的設計）。
IncrementalRunner.SaveChecksums(currentChecksums, checksumsPath);
Console.WriteLine("Completed successfully.");
