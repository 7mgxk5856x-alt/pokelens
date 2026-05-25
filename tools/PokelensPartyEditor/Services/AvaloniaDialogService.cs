using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Threading;

namespace PokelensPartyEditor.Services;

/// <summary>Avalonia 上で実装されたダイアログ Service。最小限の Window ベース実装。</summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly Window _owner;

    public AvaloniaDialogService(Window owner)
    {
        _owner = owner;
    }

    public Task ShowErrorAsync(string title, string message) =>
        ShowAsync(title, message, includeCancel: false).ContinueWith(_ => { });

    public Task<bool> ConfirmAsync(string title, string message) =>
        ShowAsync(title, message, includeCancel: true);

    private Task<bool> ShowAsync(string title, string message, bool includeCancel)
    {
        var tcs = new TaskCompletionSource<bool>();
        Dispatcher.UIThread.Post(async () =>
        {
            var dialog = new Window
            {
                Title = title,
                Width = 480,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var stack = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
            stack.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
            };
            if (includeCancel)
            {
                var yes = new Button { Content = "はい", IsDefault = true };
                yes.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
                var no = new Button { Content = "いいえ", IsCancel = true };
                no.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
                buttons.Children.Add(yes);
                buttons.Children.Add(no);
            }
            else
            {
                var ok = new Button { Content = "OK", IsDefault = true, IsCancel = true };
                ok.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
                buttons.Children.Add(ok);
            }
            stack.Children.Add(buttons);
            dialog.Content = stack;
            dialog.Closed += (_, _) => tcs.TrySetResult(includeCancel ? false : true);

            await dialog.ShowDialog(_owner);
        });
        return tcs.Task;
    }
}
