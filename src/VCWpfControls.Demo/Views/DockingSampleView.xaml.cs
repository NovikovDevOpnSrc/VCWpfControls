using System.IO;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Docking;

namespace VCWpfControls.Demo.Views;

/// <summary>Docking sample: zones, splitters, close/show, layout persistence.</summary>
public partial class DockingSampleView : UserControl
{
    private string? _savedLayout;

    /// <summary>Creates the view.</summary>
    public DockingSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "DockingSample");
    }

    private void OnSaveLayout(object sender, RoutedEventArgs e)
    {
        _savedLayout = Dock.SaveLayout();
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "vcwpf_dock_layout.xml"), _savedLayout);
    }

    private void OnRestoreLayout(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Path.GetTempPath(), "vcwpf_dock_layout.xml");
        var layout = _savedLayout ?? (File.Exists(path) ? File.ReadAllText(path) : null);
        if (layout is not null)
        {
            Dock.LoadLayout(layout);
        }
    }

    private void OnHideProperties(object sender, RoutedEventArgs e)
        => Dock.HidePane(FindPane("propertiesPane"));

    private void OnFloatProperties(object sender, RoutedEventArgs e)
    {
        var pane = FindPane("propertiesPane");
        if (pane.IsFloating)
        {
            Dock.DockPaneBack(pane);
        }
        else
        {
            Dock.FloatPane(pane);
        }
    }

    private DockPane FindPane(string name)
    {
        foreach (var pane in Dock.Panes)
        {
            if (pane.Name == name)
            {
                return pane;
            }
        }

        return Dock.Panes[0];
    }
}
