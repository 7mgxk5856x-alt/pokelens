using System.Security.Cryptography;
using System.Text.Json;
using PokelensTools.Common;
using PokelensTools.Models;

namespace PokelensTools.Pipeline;

/// <summary>入力ファイルのチェックサムを前回値と比較し、パイプラインのどのステップを再実行すべきかを判定する。</summary>
/// <remarks>
/// 変化のない入力に対する不要な再取得・再生成を避けるための差分実行機構。チェックサムは
/// <see cref="SaveChecksums"/> で永続化し、次回起動時に <see cref="LoadChecksums"/> で読み戻して比較する。
/// 入出力先は <see cref="DataPaths"/> 経由で解決するため、テストは <see cref="DataPaths.OverrideRepoRoot"/> で
/// temp dir に redirect する。
/// </remarks>
internal static class IncrementalRunner
{
    /// <summary>差分判定の結果。再実行が必要なステップの集合を表す。</summary>
    /// <remarks>各フラグは Step2（PokéAPI 取得）/ Step3（パッチ適用）/ Step4（成果物生成）の要否に対応する。</remarks>
    /// <param name="NeedsStep2">Step2（PokéAPI 翻訳取得）の再実行が必要か。</param>
    /// <param name="NeedsStep3">Step3（champions-patch 適用）の再実行が必要か。</param>
    /// <param name="NeedsStep4">Step4（成果物 JSON 生成）の再実行が必要か。</param>
    internal record Steps(bool NeedsStep2, bool NeedsStep3, bool NeedsStep4);

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>保存済みチェックサム JSON（<see cref="DataPaths.Cache.Checksums"/>）を読み込む。</summary>
    /// <remarks>
    /// ファイルが存在しない（＝初回実行）場合と、内容が JSON の null リテラルの場合に null を返す。
    /// 破損・想定外の形式では例外が伝播しうるが、checksums.json は <see cref="SaveChecksums"/> のみが書き込むキャッシュのため実用上は起きない。
    /// </remarks>
    /// <returns>読み込んだ <see cref="ChecksumSet"/>。ファイルが無い・読めない場合は null。</returns>
    internal static ChecksumSet? LoadChecksums()
    {
        string path = DataPaths.Cache.Checksums();
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ChecksumSet>(json);
    }

    /// <summary>パイプラインが扱う全入力ファイルを現在の <see cref="DataPaths"/> 配下から読み、ハッシュをまとめた <see cref="ChecksumSet"/> を返す。</summary>
    /// <remarks><see cref="LoadChecksums"/> の対となる「今回値」の計算。</remarks>
    /// <returns>11 入力ファイルのハッシュを含む <see cref="ChecksumSet"/>。存在しないファイルは空文字列ハッシュになる。</returns>
    internal static ChecksumSet ComputeCurrentChecksums()
        => new(
            ShowdownPokedex: ComputeHash(DataPaths.Cache.ShowdownPokedex()),
            ShowdownMoves: ComputeHash(DataPaths.Cache.ShowdownMoves()),
            ShowdownItems: ComputeHash(DataPaths.Cache.ShowdownItems()),
            ShowdownAbilities: ComputeHash(DataPaths.Cache.ShowdownAbilities()),
            PokeApiTranslations: ComputeHash(DataPaths.Cache.PokeApiTranslations()),
            ChampionsPatch: ComputeHash(DataPaths.Patch.Champions()),
            MovesPowerPatch: ComputeHash(DataPaths.Patch.MovesPower()),
            ItemsModifiers: ComputeHash(DataPaths.Patch.ItemsModifiers()),
            AbilitiesModifiers: ComputeHash(DataPaths.Patch.AbilitiesModifiers()),
            PokemonNamePatch: ComputeHash(DataPaths.Patch.PokemonNamePatch()),
            ItemNamePatch: ComputeHash(DataPaths.Patch.ItemNamePatch()));

    /// <summary>ファイルの SHA-256 ハッシュを小文字 16 進文字列で返す。</summary>
    /// <remarks>差分検知に用いる。ファイルが存在しない場合は空文字列を返し、例外にはしない。</remarks>
    /// <param name="filePath">ハッシュ対象のファイルパス。</param>
    /// <returns>小文字 16 進のハッシュ文字列。ファイルが無ければ空文字列。</returns>
    internal static string ComputeHash(string filePath)
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

    /// <summary>前回と今回のチェックサムを比較し、再実行すべきステップを判定する。</summary>
    /// <remarks>
    /// <paramref name="previous"/> が null（初回実行）なら全ステップを実行する。Step3 以前が再実行される場合は
    /// 必ず Step4（マージ）にも波及させる。pokeapi 翻訳や Step4 専用パッチのみの変化は Step4 だけ再実行する。
    /// </remarks>
    /// <param name="previous">前回保存したチェックサム。初回は null。</param>
    /// <param name="current">今回計算したチェックサム。</param>
    /// <returns>各ステップの要否を表す <see cref="Steps"/>。</returns>
    internal static Steps DetermineSteps(ChecksumSet? previous, ChecksumSet current)
    {
        if (previous is null)
        {
            return new Steps(true, true, true);
        }

        bool showdownChanged =
            previous.ShowdownPokedex != current.ShowdownPokedex
            || previous.ShowdownMoves != current.ShowdownMoves
            || previous.ShowdownItems != current.ShowdownItems
            || previous.ShowdownAbilities != current.ShowdownAbilities;
        bool championsPatchChanged = previous.ChampionsPatch != current.ChampionsPatch;
        bool pokeapiChanged = previous.PokeApiTranslations != current.PokeApiTranslations;
        bool step4OnlyChanged =
            previous.MovesPowerPatch != current.MovesPowerPatch
            || previous.ItemsModifiers != current.ItemsModifiers
            || previous.AbilitiesModifiers != current.AbilitiesModifiers
            || previous.PokemonNamePatch != current.PokemonNamePatch
            || previous.ItemNamePatch != current.ItemNamePatch;

        bool needsStep2 = showdownChanged;
        bool needsStep3 = showdownChanged || championsPatchChanged;
        // Step3 以前の再実行は必ず Step4 のマージにも反映する必要がある。
        // 加えて pokeapi 翻訳・Step4 専用パッチ（moves-power-patch 等）の変化も Step4 のみで再マージできる。
        bool needsStep4 = needsStep2 || needsStep3 || pokeapiChanged || step4OnlyChanged;

        return new Steps(needsStep2, needsStep3, needsStep4);
    }

    /// <summary>チェックサムを <see cref="DataPaths.Cache.Checksums"/> に整形 JSON として保存する。</summary>
    /// <remarks>出力先ディレクトリが無ければ作成する。Step2〜Step4 が例外で中断した場合は呼ばれず、次回同ステップから再実行される。</remarks>
    /// <param name="checksums">保存するチェックサム。</param>
    internal static void SaveChecksums(ChecksumSet checksums)
    {
        string path = DataPaths.Cache.Checksums();
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(checksums, SerializerOptions));
    }
}
