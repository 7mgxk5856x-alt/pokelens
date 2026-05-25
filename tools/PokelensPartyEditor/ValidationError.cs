namespace PokelensPartyEditor;

/// <summary>セーブ前のバリデーション結果 1 件分。</summary>
/// <param name="SlotIndex">エントリ位置（0-5）。全体エラーは -1。</param>
/// <param name="Field">フィールド識別子（"species" / "ability" / "item" / "nature" / "move0".."move3" / "abilityPoints" / "abilityPointsTotal"）。</param>
/// <param name="Message">ユーザー向けメッセージ。</param>
public sealed record ValidationError(int SlotIndex, string Field, string Message);
