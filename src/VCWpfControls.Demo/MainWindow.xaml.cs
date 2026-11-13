using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
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
            new TreeListSampleViewModel(),
            new EditorsSampleViewModel(),
            new DockingSampleViewModel(),
            new WizardSampleViewModel(),
            new PivotSampleViewModel(),
            new GanttSampleViewModel(),
            new SchedulerSampleViewModel(),
            new ConditionSampleViewModel(),
            new IntegrationSampleViewModel(),
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
    }

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
