using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PokelensPartyEditor.ViewModels;

namespace PokelensPartyEditor.Views;

/// <summary>1 匹分のポケモンエントリ View。能力ポイント補助ボタンの Click を ViewModel メソッドに橋渡しする。</summary>
public partial class PokemonEntryView : UserControl
{
    public PokemonEntryView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private PokemonEntryViewModel? Vm => DataContext as PokemonEntryViewModel;

    // 各能力欄の 4 ボタン（0/−/+/最大）を ViewModel に橋渡し。
    // 直接 RelayCommand にしないのは、6 欄 × 4 操作 = 24 コマンドが ViewModel を肥大化させるため。
    private void OnHpReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("hp");
    private void OnHpDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("hp");
    private void OnHpInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("hp");
    private void OnHpMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("hp");
    private void OnAtkReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("atk");
    private void OnAtkDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("atk");
    private void OnAtkInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("atk");
    private void OnAtkMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("atk");
    private void OnDefReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("def");
    private void OnDefDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("def");
    private void OnDefInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("def");
    private void OnDefMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("def");
    private void OnSpaReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("spa");
    private void OnSpaDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("spa");
    private void OnSpaInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("spa");
    private void OnSpaMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("spa");
    private void OnSpdReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("spd");
    private void OnSpdDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("spd");
    private void OnSpdInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("spd");
    private void OnSpdMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("spd");
    private void OnSpeReset(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ResetPoint("spe");
    private void OnSpeDec(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.DecrementPoint("spe");
    private void OnSpeInc(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.IncrementPoint("spe");
    private void OnSpeMax(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.MaxPoint("spe");
}
