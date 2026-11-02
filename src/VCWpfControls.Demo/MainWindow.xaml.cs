using System.Collections.ObjectModel;
using System.Windows;
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
        };
        DataContext = new MainViewModel(samples);
        NavList.SelectionChanged += (_, _) => ((MainViewModel)DataContext).SelectedSample =
            (SampleViewModelBase)NavList.SelectedItem;
    }
}

/// <summary>Holds the sample list and the selected sample.</summary>
public partial class MainViewModel
{
    /// <summary>Creates the shell view model.</summary>
    public MainViewModel(ObservableCollection<SampleViewModelBase> samples)
    {
        Samples = samples;
        _selectedSample = samples[0];
    }

    /// <summary>All available samples.</summary>
    public ObservableCollection<SampleViewModelBase> Samples { get; }

    private SampleViewModelBase _selectedSample;

    /// <summary>The currently selected sample.</summary>
    public SampleViewModelBase SelectedSample
    {
        get => _selectedSample;
        set
        {
            _selectedSample = value;
        }
    }

}
