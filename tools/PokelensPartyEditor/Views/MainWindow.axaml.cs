using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PokelensPartyEditor.ViewModels;

namespace PokelensPartyEditor.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>ViewModel の <see cref="MainWindowViewModel.ConfirmCloseAsync"/> をクローズイベントに紐付ける。</summary>
    public void AttachCloseHandler(MainWindowViewModel vm)
    {
        _vm = vm;
        Closing += async (sender, e) =>
        {
            if (_vm is null) return;
            if (!_vm.IsDirty) return;
            e.Cancel = true;
            bool ok = await _vm.ConfirmCloseAsync();
            if (ok)
            {
                _vm = null; // 再帰防止
                Close();
            }
        };
    }
}
