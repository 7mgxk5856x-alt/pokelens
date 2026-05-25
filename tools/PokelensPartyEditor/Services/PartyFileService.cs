using System.Text.Json;
using PokelensCore;
using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary>data/party.json の読み書き実装。</summary>
/// <remarks>
/// 読み取り時:
/// - ファイル不在 → <see cref="PartyFileLoadStatus.FileNotFound"/>
/// - JSON 構文不正 → <see cref="PartyFileLoadStatus.InvalidJson"/>
/// - 型不正（species: 0 など）→ <see cref="PartyFileLoadStatus.InvalidType"/>
/// - 未知キー（"hoge": "foo"）→ 無視して既知フィールドのみ反映（<see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> の既定挙動）
/// - 7 匹以上 → 7 匹目以降を切り捨て（呼び出し側 ViewModel での処理）
/// 書き込み時:
/// - インデント付き UTF-8 で書き出し（既存 JsonHelpers と同等のフォーマット）
/// - I/O 例外時は <see cref="PartyFileSaveStatus.IOError"/>
/// </remarks>
public sealed class PartyFileService : IPartyFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public PartyFileLoadResult Load()
    {
        string path = DataPaths.Master.Party();
        if (!File.Exists(path))
        {
            return new PartyFileLoadResult(
                PartyFileLoadStatus.FileNotFound,
                null,
                $"data/party.json が見つかりません ({path})");
        }

        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            return new PartyFileLoadResult(PartyFileLoadStatus.IOError, null, ex.Message);
        }

        PartyDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<PartyDocument>(content, ReadOptions);
        }
        catch (JsonException ex)
        {
            return new PartyFileLoadResult(PartyFileLoadStatus.InvalidJson, null, ex.Message);
        }
        catch (Exception ex)
        {
            // 型不正は JsonException のサブクラスとして来るが、安全網として一般例外も拾う。
            return new PartyFileLoadResult(PartyFileLoadStatus.InvalidType, null, ex.Message);
        }

        if (doc == null)
        {
            return new PartyFileLoadResult(
                PartyFileLoadStatus.InvalidJson,
                null,
                "JSON ルートが null です");
        }

        return new PartyFileLoadResult(PartyFileLoadStatus.Success, doc, null);
    }

    public PartyFileSaveResult Save(PartyDocument document)
    {
        string path = DataPaths.Master.Party();
        try
        {
            string json = JsonSerializer.Serialize(document, WriteOptions);
            File.WriteAllText(path, json);
            return new PartyFileSaveResult(PartyFileSaveStatus.Success, null);
        }
        catch (Exception ex)
        {
            return new PartyFileSaveResult(PartyFileSaveStatus.IOError, ex.Message);
        }
    }
}
