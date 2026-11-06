using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace VCWpfControls.Docking;

/// <summary>Auto-hide logic of <see cref="DockingManager"/>: edge tab strips + flyout overlay.</summary>
public partial class DockingManager
{
    private const int OpenDelayMs = 250;
    private const int CloseDelayMs = 400;

    private System.Windows.Controls.Grid? _autoHideOverlay;
    private readonly Dictionary<DockPane, Button> _autoHideTabs = new();
    private readonly Dictionary<DockZone, StackPanel> _strips = new();
    private Popup? _flyout;
    private DockPane? _flyoutPane;
    private Border? _flyoutHost;
    private TextBlock? _flyoutHeader;
    private DispatcherTimer _openTimer = null!;
    private DispatcherTimer _closeTimer = null!;

    /// <summary>Auto-hidden panes pinned to the left edge (tab strip order).</summary>
    public ObservableCollection<DockPane> AutoHiddenLeftPanes { get; } = new();

    /// <summary>Auto-hidden panes pinned to the top edge.</summary>
    public ObservableCollection<DockPane> AutoHiddenTopPanes { get; } = new();

    /// <summary>Auto-hidden panes pinned to the right edge.</summary>
    public ObservableCollection<DockPane> AutoHiddenRightPanes { get; } = new();

    /// <summary>Auto-hidden panes pinned to the bottom edge.</summary>
    public ObservableCollection<DockPane> AutoHiddenBottomPanes { get; } = new();

    /// <summary>Whether the auto-hide flyout is currently open.</summary>
    public bool IsAutoHideFlyoutOpen => _flyout?.IsOpen == true;

    /// <summary>The pane shown in the open flyout, if any.</summary>
    public DockPane? FlyoutPane => _flyoutPane;

    /// <summary>Wires auto-hide timers and strip rebuilds; called from the manager constructor.</summary>
    private void InitAutoHide()
    {
        _openTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(OpenDelayMs) };
        _closeTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(CloseDelayMs) };
        _openTimer.Tick += (_, _) => { _openTimer.Stop(); if (_pendingFlyoutPane is { } pane) { OpenAutoHideFlyout(pane); } };
        _closeTimer.Tick += (_, _) => { _closeTimer.Stop(); CloseAutoHideFlyout(); };

        foreach (var zone in new[] { DockZone.Left, DockZone.Top, DockZone.Right, DockZone.Bottom })
        {
            AutoHiddenCollectionOf(zone).CollectionChanged += (_, _) => RebuildAutoHideStrips();
        }
    }

    private DockPane? _pendingFlyoutPane;

    private ObservableCollection<DockPane> AutoHiddenCollectionOf(DockZone zone) => zone switch
    {
        DockZone.Left => AutoHiddenLeftPanes,
        DockZone.Top => AutoHiddenTopPanes,
        DockZone.Right => AutoHiddenRightPanes,
        _ => AutoHiddenBottomPanes,
    };

    private void RemoveFromAutoHideCollections(DockPane pane)
    {
        AutoHiddenLeftPanes.Remove(pane);
        AutoHiddenTopPanes.Remove(pane);
        AutoHiddenRightPanes.Remove(pane);
        AutoHiddenBottomPanes.Remove(pane);
    }

    /// <summary>Attaches the overlay grid <c>PART_AutoHideOverlay</c> from the template.</summary>
    private void AttachAutoHideOverlay()
    {
        _autoHideOverlay = GetTemplateChild("PART_AutoHideOverlay") as System.Windows.Controls.Grid;
        RebuildAutoHideStrips();
    }

    private void RebuildAutoHideStrips()
    {
        if (_autoHideOverlay is null)
        {
            return;
        }

        _autoHideOverlay.Children.Clear();
        _autoHideTabs.Clear();
        _strips.Clear();

        CreateLeftRightStrip(DockZone.Left, HorizontalAlignment.Left, -90);
        CreateLeftRightStrip(DockZone.Right, HorizontalAlignment.Right, 90);
        CreateTopBottomStrip(DockZone.Top, VerticalAlignment.Top);
        CreateTopBottomStrip(DockZone.Bottom, VerticalAlignment.Bottom);
    }

    private void CreateLeftRightStrip(DockZone zone, HorizontalAlignment alignment, double rotation)
    {
        var collection = AutoHiddenCollectionOf(zone);
        if (collection.Count == 0)
        {
            return;
        }

        var strip = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 4),
        };

        foreach (var pane in collection)
        {
            strip.Children.Add(CreateTab(pane, rotation));
        }

        _strips[zone] = strip;
        _autoHideOverlay!.Children.Add(strip);
    }

    private void CreateTopBottomStrip(DockZone zone, VerticalAlignment alignment)
    {
        var collection = AutoHiddenCollectionOf(zone);
        if (collection.Count == 0)
        {
            return;
        }

        var strip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = alignment,
            Margin = new Thickness(4, 0, 4, 0),
        };

        foreach (var pane in collection)
        {
            strip.Children.Add(CreateTab(pane, rotation: 0));
        }

        _strips[zone] = strip;
        _autoHideOverlay!.Children.Add(strip);
    }

    private Button CreateTab(DockPane pane, double rotation)
    {
        var tab = new Button
        {
            Content = HeaderText(pane),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = rotation == 0 ? new Thickness(1) : new Thickness(-4, 2, -4, 2),
            Tag = pane,
        };

        if (rotation != 0)
        {
            tab.LayoutTransform = new RotateTransform(rotation);
        }

        tab.MouseEnter += (_, _) => ScheduleFlyoutOpen(pane);
        tab.MouseLeave += (_, _) => _openTimer.Stop();
        tab.Click += (_, _) => OpenAutoHideFlyout(pane);
        _autoHideTabs[pane] = tab;
        return tab;
    }

    private static string HeaderText(DockPane pane)
        => pane.Header?.ToString() ?? string.Empty;

    private void ScheduleFlyoutOpen(DockPane pane)
    {
        _closeTimer.Stop();
        if (IsAutoHideFlyoutOpen && ReferenceEquals(_flyoutPane, pane))
        {
            return;
        }

        _pendingFlyoutPane = pane;
        _openTimer.Stop();
        _openTimer.Start();
    }

    /// <summary>Flies the auto-hidden pane out over the layout (hover or click).</summary>
    public void OpenAutoHideFlyout(DockPane pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        if (pane.Zone == DockZone.Center || !pane.IsAutoHidden)
        {
            return;
        }

        _openTimer.Stop();
        _closeTimer.Stop();
        EnsureFlyout();

        _flyoutPane = pane;
        _flyoutHeader!.Text = HeaderText(pane);
        _flyoutHost!.Child = pane;
        _flyoutHost.Width = double.NaN;
        _flyoutHost.Height = double.NaN;
        if (pane.Zone is DockZone.Left or DockZone.Right)
        {
            _flyoutHost.Width = Math.Max(pane.DockWidth.Value, 160);
        }
        else
        {
            _flyoutHost.Height = Math.Max(pane.DockHeight.Value, 120);
        }

        _flyout!.PlacementTarget = _autoHideTabs.TryGetValue(pane, out var tab) ? tab : this;
        _flyout.Placement = pane.Zone switch
        {
            DockZone.Left => PlacementMode.Right,
            DockZone.Right => PlacementMode.Left,
            DockZone.Top => PlacementMode.Bottom,
            _ => PlacementMode.Top,
        };
        _flyout.IsOpen = true;
    }

    /// <summary>Collapses the flyout (mouse leave or pin).</summary>
    public void CloseAutoHideFlyout()
    {
        _openTimer.Stop();
        _closeTimer.Stop();
        if (_flyout is { IsOpen: true })
        {
            _flyout.IsOpen = false;
        }

        // Detach the pane so it can return to its zone host without a logical-parent conflict.
        if (_flyoutHost?.Child is not null)
        {
            _flyoutHost.Child = null;
        }

        _flyoutPane = null;
    }

    private void EnsureFlyout()
    {
        if (_flyout is not null)
        {
            return;
        }

        var pin = new Button { Content = "📌", Padding = new Thickness(4), Margin = new Thickness(2) };
        pin.Click += (_, _) =>
        {
            if (_flyoutPane is { } pane)
            {
                CloseAutoHideFlyout();
                pane.IsAutoHidden = false; // SyncPane docks it back into its zone
            }
        };

        _flyoutHeader = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0),
        };

        var headerBar = new DockPanel { LastChildFill = true, Margin = new Thickness(2) };
        DockPanel.SetDock(pin, Dock.Right);
        headerBar.Children.Add(pin);
        headerBar.Children.Add(_flyoutHeader);

        _flyoutHost = new Border
        {
            Background = TryFindResource(VCWpfControls.Grid.GridResources.SurfaceBackgroundKey) as Brush ?? Brushes.White,
            BorderBrush = TryFindResource(VCWpfControls.Grid.GridResources.HeaderBorderKey) as Brush ?? Brushes.Gray,
            BorderThickness = new Thickness(1),
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(headerBar, Dock.Top);
        root.Children.Add(headerBar);
        root.Children.Add(_flyoutHost);
        _flyoutHost.Child = null;
        // The pane is inserted into _flyoutHost on open (see OpenAutoHideFlyout).

        _flyout = new Popup
        {
            Child = new Border
            {
                Background = Brushes.Transparent, // hit-test surface so MouseLeave fires reliably
                Child = root,
            },
            AllowsTransparency = true,
            StaysOpen = false,
        };
        _flyout.MouseEnter += (_, _) => _closeTimer.Stop();
        _flyout.MouseLeave += (_, _) => ScheduleFlyoutClose();
    }

    private void ScheduleFlyoutClose()
    {
        if (!IsAutoHideFlyoutOpen)
        {
            return;
        }

        _closeTimer.Stop();
        _closeTimer.Start();
    }

    /// <summary>Test hook: whether a strip tab exists for the pane.</summary>
    internal bool HasAutoHideTabForTest(DockPane pane) => _autoHideTabs.ContainsKey(pane);
}
