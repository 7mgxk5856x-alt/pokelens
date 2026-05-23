using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

/// <summary>PokéAPI からポケモン・技・特性・アイテムの日本語名を取得し、cache/ に翻訳辞書として保存する。</summary>
/// <remarks>
/// 取得は num 単位のキャッシュと並列数制限つきで行う。HTTP エラー・タイムアウト・404 は警告ログのみで握り潰し、
/// その項目を翻訳なしとして扱う（パイプライン全体を止めない）。
/// </remarks>
internal class PokeAPIFetcher
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions WriteOptions =
        new() { WriteIndented = true };
    private const int ConcurrencyLimit = 8;

    // 同一図鑑番号の各フォルム間で pokemon-species の取得を重複排除する num 単位キャッシュ
    // （例: ロトムとウォッシュロトムはどちらも species num=479 経由で解決 → HTTP 呼び出しは 1 回）。
    //
    // ライフタイム契約: FetchTranslationsAsync は PokeAPIFetcher インスタンスごとに最大 1 回の呼び出しを
    // 想定する（Program.cs は CLI 実行ごとに新しいインスタンスを生成する）。FetchTranslationsAsync 冒頭の
    // Clear() は誤った再利用に対する防御だが、前回呼び出しのタスクが進行中に契約が破られると、キャッシュが
    // 途中でクリアされ部分的な結果が観測されうる。並行・反復的な翻訳実行が要件になった場合は、このフィールドを
    // やめてローカルの ConcurrentDictionary を呼び出しチェーンに引き回す方式へ変更すること。
    private readonly ConcurrentDictionary<int, Task<JsonNode?>> _speciesCache = new();

    internal PokeAPIFetcher(HttpClient http)
    {
        _http = http;
    }

    /// <summary>ポケモン・技・特性・アイテムすべての日本語名を取得し、pokeapi-translations.json として cache/ に保存する。</summary>
    /// <remarks>PokeAPIFetcher インスタンスごとに最大 1 回の呼び出しを想定する（冒頭で内部キャッシュを Clear する）。出力先ディレクトリは無ければ作成する。</remarks>
    /// <param name="cacheDir">翻訳辞書の保存先ディレクトリ。</param>
    /// <param name="showdownPokedexPath">対象ポケモンを列挙する Showdown ポケデックスキャッシュのパス。</param>
    /// <param name="showdownMovesPath">対象技を列挙する Showdown 技キャッシュのパス。</param>
    /// <param name="showdownItemsPath">対象アイテムを列挙する Showdown アイテムキャッシュのパス。</param>
    /// <param name="showdownAbilitiesPath">対象特性を列挙する Showdown 特性キャッシュのパス。</param>
    internal async Task FetchTranslationsAsync(
        string cacheDir,
        string showdownPokedexPath,
        string showdownMovesPath,
        string showdownItemsPath,
        string showdownAbilitiesPath)
    {
        _speciesCache.Clear();

        Console.WriteLine("  Fetching Pokémon names from PokéAPI...");
        JsonObject pokemon = await FetchPokemonNamesAsync(showdownPokedexPath);

        Console.WriteLine("  Fetching move names from PokéAPI...");
        JsonObject moves = await FetchCategoryAsync(
            showdownMovesPath,
            Endpoints.PokeApi.Move,
            "moves");

        Console.WriteLine("  Fetching ability names from PokéAPI...");
        JsonObject abilities = await FetchCategoryAsync(
            showdownAbilitiesPath,
            Endpoints.PokeApi.Ability,
            "abilities");

        Console.WriteLine("  Fetching item names from PokéAPI...");
        JsonObject items = await FetchItemNamesAsync(showdownItemsPath);

        var translations = new JsonObject
        {
            [TranslationKey.Pokemon] = pokemon,
            [TranslationKey.Moves] = moves,
            [TranslationKey.Abilities] = abilities,
            [TranslationKey.Items] = items,
        };

        Directory.CreateDirectory(cacheDir);
        File.WriteAllText(
            Path.Combine(cacheDir, CacheFileName.PokeApiTranslations),
            translations.ToJsonString(WriteOptions));
    }

    // フォルム単位の日本語名を解決するポケモン専用 fetcher。
    // - 基本エントリ（forme なし）→ species 名（例: "ロトム"）。
    // - フォルム違いのエントリ → pokemon-form の form_names。PokéAPI はフォルム修飾語だけを返すことがある
    //   （例: ザシアン-キング型の "けんのおう"）ため、フォルム名に species 名が含まれない場合は
    //   "species (form)" の形で結合する。
    private async Task<JsonObject> FetchPokemonNamesAsync(string showdownPokedexPath)
    {
        JsonObject showdownData = JsonNode.Parse(File.ReadAllText(showdownPokedexPath))!.AsObject();

        var targets = new List<(string Key, int Num, string Name, string? Forme)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry)
            {
                continue;
            }

            int num = entry[ShowdownKey.Num]?.GetValue<int>() ?? 0;
            if (num <= 0)
            {
                continue;
            }

            string? englishName = entry[ShowdownKey.Name]?.GetValue<string>();
            if (string.IsNullOrEmpty(englishName))
            {
                continue;
            }

            string? forme = entry[ShowdownKey.Pokedex.Forme]?.GetValue<string>();
            targets.Add((key, num, englishName, forme));
        }

        return await RunParallelAsync(targets, async t =>
        {
            string? ja = await ResolvePokemonJapaneseNameAsync(t.Name, t.Num, t.Forme);
            if (ja == null)
            {
                string slug = PokeApiSlug.PokemonFormSlug(t.Name);
                Console.WriteLine(
                    $"    Warning: no Japanese name for pokemon/{t.Key} (id={t.Num}, slug={slug})");
            }
            return (t.Key, ja);
        });
    }

    private async Task<string?> ResolvePokemonJapaneseNameAsync(
        string showdownName, int num, string? forme)
    {
        string? speciesName = await GetSpeciesJaNameAsync(num);

        if (string.IsNullOrEmpty(forme))
        {
            return speciesName;
        }

        string? formName = await GetFormJaNameAsync(showdownName, num);
        if (string.IsNullOrEmpty(formName))
        {
            return speciesName;
        }

        if (!string.IsNullOrEmpty(speciesName) && formName.Contains(speciesName))
        {
            return formName;
        }

        if (!string.IsNullOrEmpty(speciesName))
        {
            return $"{speciesName} ({formName})";
        }

        return formName;
    }

    private async Task<string?> GetSpeciesJaNameAsync(int num)
    {
        JsonNode? node = await GetSpeciesNodeAsync(num);
        return node == null ? null : PokeApiName.ExtractJa(node, PokeApiKey.Names);
    }

    private Task<JsonNode?> GetSpeciesNodeAsync(int num)
    {
        return _speciesCache.GetOrAdd(num, async n =>
        {
            string? body = await FetchTextOrNullAsync(Endpoints.PokeApi.PokemonSpecies(n));
            return body == null ? null : JsonNode.Parse(body);
        });
    }

    // まず Showdown 由来の slug を試し、外れたら pokemon-species の varieties を走査して
    // 一致する slug を探す（例: Showdown "ogerponwellspring" → "Ogerpon-Wellspring" →
    // slug "ogerpon-wellspring" → PokéAPI variety "ogerpon-wellspring-mask"）。
    private async Task<string?> GetFormJaNameAsync(string showdownName, int num)
    {
        string slug = PokeApiSlug.PokemonFormSlug(showdownName);
        JsonNode? node = await FetchFormNodeAsync(slug);
        if (node != null)
        {
            string? ja = PokeApiName.ExtractJa(node, PokeApiKey.FormNames);
            if (!string.IsNullOrEmpty(ja))
            {
                return ja;
            }
        }

        JsonNode? species = await GetSpeciesNodeAsync(num);
        if (species == null)
        {
            return null;
        }

        string? matched = PokeApiName.FindMatchingVariety(species, slug);
        if (matched == null || matched == slug)
        {
            return null;
        }

        JsonNode? fallback = await FetchFormNodeAsync(matched);
        if (fallback == null)
        {
            return null;
        }

        return PokeApiName.ExtractJa(fallback, PokeApiKey.FormNames);
    }

    private async Task<JsonNode?> FetchFormNodeAsync(string slug)
    {
        string? body = await FetchTextOrNullAsync(Endpoints.PokeApi.PokemonForm(slug));
        return body == null ? null : JsonNode.Parse(body);
    }

    // slug ベースのアイテム名 fetcher。Showdown の数値 ID は PokéAPI のものと食い違うため、
    // ハイフン区切りの小文字名で解決する。
    private async Task<JsonObject> FetchItemNamesAsync(string showdownItemsPath)
    {
        JsonObject showdownData = JsonNode.Parse(File.ReadAllText(showdownItemsPath))!.AsObject();

        var targets = new List<(string Key, string Name)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry)
            {
                continue;
            }

            int num = entry[ShowdownKey.Num]?.GetValue<int>() ?? 0;
            if (num <= 0)
            {
                continue;
            }

            string? name = entry[ShowdownKey.Name]?.GetValue<string>();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            targets.Add((key, name));
        }

        return await RunParallelAsync(targets, async t =>
        {
            string slug = PokeApiSlug.ItemSlug(t.Name);
            string? ja = await FetchJapaneseNameAsync(Endpoints.PokeApi.Item(slug));
            if (ja == null)
            {
                Console.WriteLine($"    Warning: no Japanese name for items/{t.Key} (slug={slug})");
            }

            return (t.Key, ja);
        });
    }

    private async Task<JsonObject> FetchCategoryAsync(
        string showdownCachePath,
        Func<int, string> urlBuilder,
        string categoryName)
    {
        JsonObject showdownData = JsonNode.Parse(File.ReadAllText(showdownCachePath))!.AsObject();

        var targets = new List<(string Key, int Num)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry)
            {
                continue;
            }

            int num = entry[ShowdownKey.Num]?.GetValue<int>() ?? 0;
            if (num <= 0)
            {
                continue;
            }

            targets.Add((key, num));
        }

        return await RunParallelAsync(targets, async t =>
        {
            string? ja = await FetchJapaneseNameAsync(urlBuilder(t.Num));
            if (ja == null)
            {
                Console.WriteLine($"    Warning: no Japanese name for {categoryName}/{t.Key} (id={t.Num})");
            }

            return (t.Key, ja);
        });
    }

    // 並列数を制限しつつ resolver を全ターゲットに適用し、非 null の結果を resolver の Key を
    // キーとする JsonObject に集約する。3 つの FetchXxxAsync で共通する SemaphoreSlim による
    // 同時実行制御と結果集約を一元化する。
    private async Task<JsonObject> RunParallelAsync<T>(
        IReadOnlyList<T> targets,
        Func<T, Task<(string Key, string? JaName)>> resolver)
    {
        using var gate = new SemaphoreSlim(ConcurrencyLimit);
        var resolved = new (string Key, string? JaName)[targets.Count];
        var tasks = new Task[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            int idx = i;
            T t = targets[idx];
            tasks[idx] = Task.Run(async () =>
            {
                await gate.WaitAsync();
                try { resolved[idx] = await resolver(t); }
                finally { gate.Release(); }
            });
        }
        await Task.WhenAll(tasks);

        var result = new JsonObject();
        foreach (var (key, ja) in resolved)
        {
            if (ja != null)
            {
                result[key] = ja;
            }
        }
        return result;
    }

    /// <summary>指定した PokéAPI リソース URL を取得し、その names 配列から日本語名を返す。</summary>
    /// <remarks>取得失敗（HTTP エラー・タイムアウト・404・パース不能）の場合は例外にせず null を返す。</remarks>
    /// <param name="url">取得対象の PokéAPI リソース URL。</param>
    /// <returns>日本語名。取得・抽出できなければ null。</returns>
    internal async Task<string?> FetchJapaneseNameAsync(string url)
    {
        string? body = await FetchTextOrNullAsync(url);
        if (body == null)
        {
            return null;
        }

        var node = JsonNode.Parse(body);
        if (node == null)
        {
            return null;
        }

        return PokeApiName.ExtractJa(node, PokeApiKey.Names);
    }

    private async Task<string?> FetchTextOrNullAsync(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (TaskCanceledException)
        {
            // HttpClient はリクエストのタイムアウトを TaskCanceledException として通知する。
            // CancellationToken は渡していないため、ここで外部からのキャンセルは発生しない。
            Console.WriteLine($"    Warning: HTTP timeout for {url}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"    Warning: HTTP error for {url}: {ex.Message}");
            return null;
        }

        // 404 はリソース不在で正常な fallback ケースなので無言で null。
        // 400 は slug 形式の不備など呼び出し側のバグを示すので Warning を残す。
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            Console.WriteLine($"    Warning: 400 Bad Request for {url} — slug 形式に問題がある可能性");
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"    Warning: {response.StatusCode} for {url}");
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

}
