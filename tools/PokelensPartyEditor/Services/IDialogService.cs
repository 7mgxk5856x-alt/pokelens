namespace PokelensPartyEditor.Services;

/// <summary>ダイアログ表示を抽象化する Service。ViewModel テスト時はモック差し替え。</summary>
public interface IDialogService
{
    /// <summary>エラーダイアログを表示する。OK のみ。</summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>確認ダイアログを表示する。「はい / いいえ」。</summary>
    /// <returns>「はい」なら true、「いいえ」または × なら false。</returns>
    Task<bool> ConfirmAsync(string title, string message);
}
