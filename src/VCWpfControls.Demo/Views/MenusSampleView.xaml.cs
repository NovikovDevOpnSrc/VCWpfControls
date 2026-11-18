using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Menus sample: the themed main menu bar and context menu.</summary>
public partial class MenusSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public MenusSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "MenusSample");
        DataContext = this;
    }

    /// <summary>Identifies the <see cref="LastAction"/> dependency property.</summary>
    public static readonly DependencyProperty LastActionProperty = DependencyProperty.Register(
        nameof(LastAction), typeof(string), typeof(MenusSampleView),
        new PropertyMetadata("Open a menu (Alt+F), click an item, right-click the area below."));

    /// <summary>The last menu interaction (status line; a DP so the binding updates).</summary>
    public string LastAction
    {
        get => (string)GetValue(LastActionProperty);
        private set => SetValue(LastActionProperty, value);
    }

    private void Report(string action) => LastAction = action;

    private void OnNewClick(object sender, RoutedEventArgs e) => Report("File → New (Ctrl+N)");

    private void OnOpenClick(object sender, RoutedEventArgs e) => Report("File → Open… (Ctrl+O)");

    private void OnExitClick(object sender, RoutedEventArgs e) => Report("File → Exit (Alt+F4)");

    private void OnEditClick(object sender, RoutedEventArgs e)
        => Report($"Edit → {((MenuItem)sender).Header} / autosave={((MenuItem)sender).IsCheckable && ((MenuItem)sender).IsChecked}");

    private void OnAutosaveChanged(object sender, RoutedEventArgs e)
        => Report($"Edit → Autosave = {((MenuItem)sender).IsChecked}");

    private void OnSendClick(object sender, RoutedEventArgs e) => Report($"View → {((MenuItem)sender).Header}");

    private void OnAboutClick(object sender, RoutedEventArgs e) => Report("Help → About");

    private void OnRefreshClick(object sender, RoutedEventArgs e) => Report("Context → Refresh (F5)");

    private void OnSortClick(object sender, RoutedEventArgs e)
        => Report($"Context → {((MenuItem)sender).Header}");
}
