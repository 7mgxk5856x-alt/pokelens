using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PokelensPartyEditor.Services;
using PokelensPartyEditor.ViewModels;
using PokelensPartyEditor.Views;

namespace PokelensPartyEditor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 手動 DI（個人ローカル使用のため DI コンテナは使わない）
            var masterData = new MasterDataService();
            var suggest = new SuggestService(masterData);
            var fileService = new PartyFileService();

            var window = new MainWindow();
            var dialog = new AvaloniaDialogService(window);
            var vm = new MainWindowViewModel(fileService, dialog, masterData, suggest);
            window.DataContext = vm;
            window.AttachCloseHandler(vm);
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
