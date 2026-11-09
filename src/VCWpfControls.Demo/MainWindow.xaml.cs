using System.Collections.ObjectModel;
using System.Windows;
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
    }

    private void OnSettingsClick(object sender, System.Windows.RoutedEventArgs e)
        => SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
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
