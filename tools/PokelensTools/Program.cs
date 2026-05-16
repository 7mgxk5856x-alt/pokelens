using PokelensTools;

// Resolve repo root: when run via "dotnet run --project tools/PokelensTools" from repo root,
// the working directory IS the repo root.
var repoRoot = Directory.GetCurrentDirectory();
var cacheDir = Path.Combine(repoRoot, "cache");
var dataDir = Path.Combine(repoRoot, "data");
var toolsDir = Path.Combine(repoRoot, "tools", "PokelensTools");

var checksumsPath       = Path.Combine(cacheDir, "checksums.json");
var pokedexCachePath    = Path.Combine(cacheDir, "showdown-pokedex.json");
var movesCachePath      = Path.Combine(cacheDir, "showdown-moves.json");
var itemsCachePath      = Path.Combine(cacheDir, "showdown-items.json");
var abilitiesCachePath  = Path.Combine(cacheDir, "showdown-abilities.json");
var translationsPath    = Path.Combine(cacheDir, "pokeapi-translations.json");

var championsPatchPath    = Path.Combine(toolsDir, "champions-patch.json");
var movesPowerPatchPath   = Path.Combine(toolsDir, "moves-power-patch.json");
var itemsModifiersPath    = Path.Combine(toolsDir, "items-modifiers.json");
var abilitiesModifiersPath = Path.Combine(toolsDir, "abilities-modifiers.json");

// Step 1: Always fetch Showdown data
Console.WriteLine("[Step 1] Fetching Showdown data...");
await ShowdownFetcher.FetchAll(cacheDir);
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
};

var old = IncrementalRunner.LoadChecksums(checksumsPath);
var steps = IncrementalRunner.DetermineSteps(old, current);

if (!steps.NeedsStep2 && !steps.NeedsStep3 && !steps.NeedsStep4)
{
    Console.WriteLine("No changes detected. data/ is up to date.");
    return;
}

// Step 2: Fetch PokéAPI translations (if needed)
if (steps.NeedsStep2)
{
    Console.WriteLine("[Step 2] Fetching PokéAPI translations...");
    await PokeAPIFetcher.FetchTranslations(
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
        dataDir);
    Console.WriteLine("  Done.");
}

// Update checksums
IncrementalRunner.SaveChecksums(current, checksumsPath);
Console.WriteLine("Completed successfully.");
