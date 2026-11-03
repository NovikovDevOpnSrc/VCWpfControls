using System.Windows;

namespace VCWpfControls.TreeList;

/// <summary>Event arguments of <see cref="TreeListView.NodeCheckChanged"/>.</summary>
public sealed class NodeCheckChangedEventArgs : RoutedEventArgs
{
    /// <summary>Creates the event args.</summary>
    public NodeCheckChangedEventArgs(object item, bool isChecked)
    {
        Item = item;
        IsChecked = isChecked;
    }

    /// <summary>The node item whose check box was toggled (after the cascade).</summary>
    public object Item { get; }

    /// <summary>The state applied to the node (and its descendants).</summary>
    public bool IsChecked { get; }
}
