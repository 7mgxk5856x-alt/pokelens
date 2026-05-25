using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary>ロード結果。成功/失敗を明示的に区別し、失敗時はエラーカテゴリで分岐できるようにする。</summary>
public enum PartyFileLoadStatus
{
    Success,
    FileNotFound,
    InvalidJson,
    InvalidType,
    IOError,
}

/// <summary>ロード結果。</summary>
/// <param name="Status">成否カテゴリ。</param>
/// <param name="Document">成功時のドキュメント（失敗時は null）。</param>
/// <param name="ErrorMessage">失敗時のメッセージ（例外メッセージを含む）。成功時は null。</param>
public sealed record PartyFileLoadResult(
    PartyFileLoadStatus Status,
    PartyDocument? Document,
    string? ErrorMessage);

/// <summary>セーブ結果。</summary>
public enum PartyFileSaveStatus
{
    Success,
    IOError,
}

/// <summary>セーブ結果。</summary>
public sealed record PartyFileSaveResult(
    PartyFileSaveStatus Status,
    string? ErrorMessage);
