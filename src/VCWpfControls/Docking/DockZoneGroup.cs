using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VCWpfControls.Docking;

/// <summary>
/// A stack of <see cref="DockZoneHost"/>s separated by <see cref="GridSplitter"/>s —
/// nested splitter groups inside ONE docking zone. Panes with the same
/// <see cref="DockPane.SplitGroup"/> share a host (tabbed); different groups stack with an
/// independent splitter between them. Left/right zones stack vertically, top/bottom
/// zones stack horizontally.
/// </summary>
public sealed class DockZoneGroup : ContentControl
{
    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(DockZoneGroup),
        new PropertyMetadata(Orientation.Vertical, (d, _) => ((DockZoneGroup)d).Rebuild()));

    private readonly ObservableCollection<DockZoneHost> _hosts = new();
    private readonly System.Windows.Controls.Grid _root = new();

    static DockZoneGroup()
    {
    }

    /// <summary>Creates the stack.</summary>
    public DockZoneGroup()
    {
        _hosts.CollectionChanged += (_, _) => Rebuild();
        Content = _root;
    }

    /// <summary>Stack direction: vertical = hosts above/below (left/right zones).</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The hosts in stack order.</summary>
    public ObservableCollection<DockZoneHost> Hosts => _hosts;

    /// <summary>Whether any host shows a pane (drives the outer zone visibility).</summary>
    public bool HasVisiblePanes => _hosts.Any(h => h.HasVisiblePanes);

    private void Rebuild()
    {
        _root.RowDefinitions.Clear();
        _root.ColumnDefinitions.Clear();
        _root.Children.Clear();

        var vertical = Orientation == Orientation.Vertical;
        if (vertical)
        {
            for (var i = 0; i < _hosts.Count; i++)
            {
                _root.RowDefinitions.Add(new RowDefinition
                {
                    Height = i == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto),
                    MinHeight = 24,
                });
            }
        }
        else
        {
            for (var i = 0; i < _hosts.Count; i++)
            {
                _root.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = i == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto),
                    MinWidth = 24,
                });
            }
        }

        for (var i = 0; i < _hosts.Count; i++)
        {
            var host = _hosts[i];
            if (vertical)
            {
                System.Windows.Controls.Grid.SetRow(host, i * 2);
                System.Windows.Controls.Grid.SetColumn(host, 0);
                _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                System.Windows.Controls.Grid.SetRowSpan(host, 1);
            }
            else
            {
                System.Windows.Controls.Grid.SetColumn(host, i * 2);
                System.Windows.Controls.Grid.SetRow(host, 0);
                _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                System.Windows.Controls.Grid.SetColumnSpan(host, 1);
            }

            _root.Children.Add(host);
            if (i < _hosts.Count - 1)
            {
                var splitter = new GridSplitter
                {
                    Width = vertical ? double.NaN : 4,
                    Height = vertical ? 4 : double.NaN,
                    HorizontalAlignment = vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Center,
                    VerticalAlignment = vertical ? VerticalAlignment.Center : VerticalAlignment.Stretch,
                    ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                    ResizeDirection = vertical ? GridResizeDirection.Rows : GridResizeDirection.Columns,
                    Background = System.Windows.Media.Brushes.Transparent,
                };
                if (vertical)
                {
                    _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });
                    System.Windows.Controls.Grid.SetRow(splitter, i * 2 + 1);
                    System.Windows.Controls.Grid.SetColumn(splitter, 0);
                }
                else
                {
                    _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
                    System.Windows.Controls.Grid.SetColumn(splitter, i * 2 + 1);
                    System.Windows.Controls.Grid.SetRow(splitter, 0);
                }

                _root.Children.Add(splitter);
            }
        }
    }
}
