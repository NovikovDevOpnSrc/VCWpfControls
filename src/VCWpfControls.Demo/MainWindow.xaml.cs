using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using VCWpfControls.Demo.ViewModels;

namespace VCWpfControls.Demo;

/// <summary>Main shell: navigation between the sample pages.</summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the window and its sample list.</summary>
    public MainWindow()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "MainWindow");
        var samples = new ObservableCollection<SampleViewModelBase>
        {
            new BasicSampleViewModel(),
            new ColumnTypesSampleViewModel(),
            new SortingSampleViewModel(),
            new SelectionSampleViewModel(),
            new EditingSampleViewModel(),
            new VirtualizationSampleViewModel(),
            new AnalyticsSampleViewModel(),
            new PropertyGridSampleViewModel(),
            new PropertyExplorerSampleViewModel(),
            new TreeListSampleViewModel(),
            new EditorsSampleViewModel(),
            new DockingSampleViewModel(),
            new WizardSampleViewModel(),
            new PivotSampleViewModel(),
            new GanttSampleViewModel(),
            new SchedulerSampleViewModel(),
            new ConditionSampleViewModel(),
            new IntegrationSampleViewModel(),
            new MenusSampleViewModel(),
            new ChromeSampleViewModel(),
        };
        DataContext = new MainViewModel(samples);

        // The settings flyout keeps its own view model (loads and applies the persisted
        // settings — the same values App.OnStartup applied at boot).
        SettingsFlyout.DataContext = new SettingsSampleViewModel();

        // System mode: follow the OS light/dark and high-contrast switches live (the VM
        // re-applies only when its own values change; this covers the OS side).
        Services.SystemThemeWatcher.Start();
        Services.SystemThemeWatcher.LightChanged += OnSystemThemeFlip;
        Services.SystemThemeWatcher.HighContrastChanged += OnSystemThemeFlip;

        // Status bar: the current page + the live theme (follows ThemeManager swaps).
        if (DataContext is MainViewModel main)
        {
            main.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedSample))
                {
                    StatusBarPage.Text = main.SelectedSample?.Title ?? string.Empty;
                }
            };
            StatusBarPage.Text = main.SelectedSample?.Title ?? string.Empty;
        }

        UpdateStatusBarTheme();
        Services.SystemThemeWatcher.LightChanged += (_, _) => UpdateStatusBarTheme();
        Services.SystemThemeWatcher.HighContrastChanged += (_, _) => UpdateStatusBarTheme();
        Themes.ThemeManager.ThemeChanged += (_, _) => UpdateStatusBarTheme();

        // Blank-header diagnostics: the exe stamps itself (old builds are visually
        // identical — a stamp in the title answers "which build is on screen"), and the
        // grid segment re-measures the header band every second: captions wider than 0
        // with blank pixels on screen = raster-level failure; width 0 = content absent.
        var exePath = Environment.ProcessPath ?? string.Empty;
        var stamp = File.Exists(exePath)
            ? File.GetLastWriteTimeUtc(exePath).ToString("MMdd-HHmm", System.Globalization.CultureInfo.InvariantCulture)
            : "n/a";
        Title = $"VCWpfControls Demo — built {stamp}UTC";
        var timer = new DispatcherTimer(System.TimeSpan.FromSeconds(1),
            DispatcherPriority.Background, (_, _) => UpdateStatusBarGrid(), Dispatcher);
        timer.Start();
        UpdateStatusBarGrid();
    }

    private void UpdateStatusBarGrid()
    {
        var content = Content as System.Windows.DependencyObject;
        var grid = content is null ? null : FindVisualChild<VCWpfControls.Grid.GridControl>(content);
        if (grid is null)
        {
            StatusBarGrid.Text = string.Empty;
            return;
        }

        var captions = new List<double>();
        foreach (var header in FindVisualChildren<VCWpfControls.Grid.Primitives.GridColumnHeader>(grid))
        {
            var width = 0d;
            foreach (var text in FindVisualChildren<System.Windows.Controls.TextBlock>(header))
            {
                width = Math.Max(width, text.ActualWidth);
            }

            captions.Add(width);
        }

        if (captions.Count == 0)
        {
            StatusBarGrid.Text = "grid: no headers";
            return;
        }

        StatusBarGrid.Text = $"grid: {captions.Count} cols, captions {captions.Min():F0}px";
    }


    private static T? FindVisualChild<T>(System.Windows.DependencyObject root)
        where T : System.Windows.DependencyObject
    {
        foreach (var found in FindVisualChildren<T>(root))
        {
            return found;
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject root)
        where T : System.Windows.DependencyObject
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var deeper in FindVisualChildren<T>(child))
            {
                yield return deeper;
            }
        }
    }

    private void UpdateStatusBarTheme()
        => StatusBarTheme.Text = $"Theme: {Themes.ThemeManager.CurrentTheme}";

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && SettingsPopup.IsOpen)
        {
            SettingsPopup.IsOpen = false; // Esc closes the flyout (guidelines: Esc for panels)
            e.Handled = true;
        }
    }

    private void OnSettingsClick(object sender, System.Windows.RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        if (SettingsPopup.IsOpen)
        {
            // The popup is a separate focus scope — move the keyboard into it, or the
            // whole settings surface stays mouse-only (acceptance round finding).
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                () => SettingsFlyout.FocusFirstOption());
        }
    }

    private void OnSystemThemeFlip(object? sender, bool e)
    {
        if (SettingsFlyout.DataContext is ViewModels.SettingsSampleViewModel vm && vm.Theme == "System")
        {
            vm.ReapplyFromSystem();
        }
    }
}

/// <summary>Holds the sample list and the selected sample (raises change notifications).</summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>Creates the shell view model.</summary>
    public MainViewModel(ObservableCollection<SampleViewModelBase> samples)
    {
        Samples = samples;
        _selectedSample = samples[0];
    }

    /// <summary>All available samples.</summary>
    public ObservableCollection<SampleViewModelBase> Samples { get; }

    [ObservableProperty]
    private SampleViewModelBase _selectedSample;
}
