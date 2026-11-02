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
        };
        DataContext = new MainViewModel(samples);
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
