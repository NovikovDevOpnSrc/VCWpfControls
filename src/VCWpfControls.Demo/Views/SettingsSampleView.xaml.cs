using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Demo.ViewModels;

namespace VCWpfControls.Demo.Views;

/// <summary>Global demo settings: theme, accent, language.</summary>
public partial class SettingsSampleView : UserControl
{
    /// <summary>Creates the page.</summary>
    public SettingsSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SettingsSample");
    }

    /// <summary>
    /// Moves keyboard focus to the first theme option — the shell calls this when the
    /// flyout opens (a popup is a separate focus scope; without the move the whole
    /// settings surface is mouse-only).
    /// </summary>
    public void FocusFirstOption()
    {
        if (FindDescendant<RadioButton>(this) is { } first)
        {
            first.Focus();
        }
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T matched)
            {
                return matched;
            }

            if (FindDescendant<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private void OnReset(object sender, RoutedEventArgs e)
        => ((SettingsSampleViewModel)DataContext).Reset();
}
