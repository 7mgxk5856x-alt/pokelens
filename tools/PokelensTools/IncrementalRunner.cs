using System.Security.Cryptography;
using System.Text.Json;

namespace PokelensTools;

internal static class IncrementalRunner
{
    public record Steps(bool NeedsStep2, bool NeedsStep3, bool NeedsStep4);

    private static readonly string[] ShowdownKeys =
        ["showdown-pokedex", "showdown-moves", "showdown-items", "showdown-abilities"];

    private static readonly string[] Step4OnlyKeys =
        ["moves-power-patch", "items-modifiers", "abilities-modifiers", "pokemon-name-patch", "item-name-patch"];

    public static Dictionary<string, string> LoadChecksums(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    public static string ComputeHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using var sha = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static Steps DetermineSteps(
        Dictionary<string, string> old,
        Dictionary<string, string> current)
    {
        if (old.Count == 0)
        {
            return new Steps(true, true, true);
        }

        bool showdownChanged = ShowdownKeys.Any(k =>
            old.GetValueOrDefault(k) != current.GetValueOrDefault(k));
        bool championsPatchChanged =
            old.GetValueOrDefault("champions-patch") != current.GetValueOrDefault("champions-patch");
        bool pokeapiChanged =
            old.GetValueOrDefault("pokeapi-translations") != current.GetValueOrDefault("pokeapi-translations");
        bool step4OnlyChanged = Step4OnlyKeys.Any(k =>
            old.GetValueOrDefault(k) != current.GetValueOrDefault(k));

        bool needsStep2 = showdownChanged;
        bool needsStep3 = showdownChanged || championsPatchChanged;
        // Step3 以前の再実行は必ず Step4 のマージにも反映する必要がある。
        // 加えて pokeapi 翻訳・Step4 専用パッチ（moves-power-patch 等）の変化も Step4 のみで再マージできる。
        bool needsStep4 = needsStep2 || needsStep3 || pokeapiChanged || step4OnlyChanged;

        return new Steps(needsStep2, needsStep3, needsStep4);
    }

    public static void SaveChecksums(Dictionary<string, string> checksums, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(checksums,
            new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
    }
}
