using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;
using VCWpfControls.Gantt;

namespace VCWpfControls.Demo.Views;

/// <summary>Gantt sample: the view wired to the sample plan, reporting drag/link actions.</summary>
public partial class GanttSampleView : UserControl
{
    /// <summary>Creates the page.</summary>
    public GanttSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "GanttSample");
    }

    private void OnTaskEdited(object sender, GanttTask task)
        => ((GanttSampleViewModel)DataContext).LastAction =
            $"Moved/resized '{task.Name}' → {task.Start:d}..{task.Finish:d}";

    private void OnLinkCreated(object sender, GanttDependency dependency)
        => ((GanttSampleViewModel)DataContext).LastAction =
            $"Linked {dependency.PredecessorId} → {dependency.SuccessorId} ({dependency.Type})";

    private void OnReset(object sender, RoutedEventArgs e)
        => ((GanttSampleViewModel)DataContext).Reset();
}
