using PokelensTools;

// Resolve repo root: when run via "dotnet run --project tools/PokelensTools" from repo root,
// the working directory IS the repo root.
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
string pokedexCachePath    = Path.Combine(cacheDir, "showdown-pokedex.json");
string movesCachePath      = Path.Combine(cacheDir, "showdown-moves.json");
string itemsCachePath      = Path.Combine(cacheDir, "showdown-items.json");
string abilitiesCachePath  = Path.Combine(cacheDir, "showdown-abilities.json");
string translationsPath    = Path.Combine(cacheDir, "pokeapi-translations.json");

string championsPatchPath    = Path.Combine(toolsDir, "champions-patch.json");
string movesPowerPatchPath   = Path.Combine(toolsDir, "moves-power-patch.json");
string itemsModifiersPath    = Path.Combine(toolsDir, "items-modifiers.json");
string abilitiesModifiersPath = Path.Combine(toolsDir, "abilities-modifiers.json");
string pokemonNamePatchPath  = Path.Combine(toolsDir, "pokemon-name-patch.json");
string itemNamePatchPath     = Path.Combine(toolsDir, "item-name-patch.json");

// Shared HttpClient: injected into both fetchers so they no longer hold their own
// static instances. Lifetime is bound to Program.cs and the instance is disposed on exit.
using var http = new HttpClient();
var showdownFetcher = new ShowdownFetcher(http);
var pokeApiFetcher = new PokeAPIFetcher(http);

// Step 1: Always fetch Showdown data
Console.WriteLine("[Step 1] Fetching Showdown data...");
await showdownFetcher.FetchAllAsync(cacheDir);
Console.WriteLine("  Done.");

// Compute current checksums for incremental run decision
var current = new Dictionary<string, string>
{
    ["showdown-pokedex"]   = IncrementalRunner.ComputeHash(pokedexCachePath),
    ["showdown-moves"]     = IncrementalRunner.ComputeHash(movesCachePath),
    ["showdown-items"]     = IncrementalRunner.ComputeHash(itemsCachePath),
    ["showdown-abilities"] = IncrementalRunner.ComputeHash(abilitiesCachePath),
    ["pokeapi-translations"] = IncrementalRunner.ComputeHash(translationsPath),
    ["champions-patch"]    = IncrementalRunner.ComputeHash(championsPatchPath),
    ["moves-power-patch"]  = IncrementalRunner.ComputeHash(movesPowerPatchPath),
    ["items-modifiers"]    = IncrementalRunner.ComputeHash(itemsModifiersPath),
    ["abilities-modifiers"] = IncrementalRunner.ComputeHash(abilitiesModifiersPath),
    ["pokemon-name-patch"] = IncrementalRunner.ComputeHash(pokemonNamePatchPath),
    ["item-name-patch"]    = IncrementalRunner.ComputeHash(itemNamePatchPath),
};

Dictionary<string, string> old = IncrementalRunner.LoadChecksums(checksumsPath);
IncrementalRunner.Steps steps = IncrementalRunner.DetermineSteps(old, current);

if (!steps.NeedsStep2 && !steps.NeedsStep3 && !steps.NeedsStep4)
{
    Console.WriteLine("No changes detected. data/ is up to date.");
    return;
}

// Step 2: Fetch PokéAPI translations (if needed)
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

    // Update pokeapi-translations hash after fetch
    current["pokeapi-translations"] = IncrementalRunner.ComputeHash(translationsPath);
}

// Step 3: Apply champions-patch.json (if needed)
if (steps.NeedsStep3)
{
    Console.WriteLine("[Step 3] Applying champions-patch.json...");
    PatchApplicator.Apply(pokedexCachePath, movesCachePath, championsPatchPath);
    Console.WriteLine("  Done.");
}

// Step 4: Generate final output
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

// Update checksums
// Step2〜Step4 が例外で終了した場合は SaveChecksums に到達しないため、
// 次回起動で同ステップから再実行される（部分失敗の回復性を担保するための意図的設計）。
IncrementalRunner.SaveChecksums(current, checksumsPath);
Console.WriteLine("Completed successfully.");
