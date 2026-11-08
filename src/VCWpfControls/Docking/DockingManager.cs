using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;

namespace VCWpfControls.Docking;

/// <summary>
/// Root docking container: hosts <see cref="DockPane"/> items in five zones (left/top/right/
/// bottom with resizable splitters, center tabbed). Supports hiding panes and layout
/// persistence via <see cref="SaveLayout"/>/<see cref="LoadLayout"/>.
/// </summary>
[ContentProperty(nameof(Panes))]
public partial class DockingManager : Control
{
    /// <summary>Identifies the <see cref="ActivePane"/> dependency property.</summary>
    public static readonly DependencyProperty ActivePaneProperty = DependencyProperty.Register(
        nameof(ActivePane), typeof(DockPane), typeof(DockingManager),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnActivePaneChanged));

    private const string PartLeftHost = "PART_LeftHost";
    private const string PartTopHost = "PART_TopHost";
    private const string PartRightHost = "PART_RightHost";
    private const string PartBottomHost = "PART_BottomHost";
    private const string PartCenterHost = "PART_CenterHost";
    private const string PartLeftColumn = "PART_LeftColumn";
    private const string PartRightColumn = "PART_RightColumn";
    private const string PartTopRow = "PART_TopRow";
    private const string PartBottomRow = "PART_BottomRow";

    private readonly ObservableCollection<DockPane> _panes = new();
    private readonly Dictionary<DockPane, Window> _floatingWindows = new();

    /// <summary>Shared converter hiding zone splitters of empty zones (template use).</summary>
    public static readonly System.Windows.Data.IValueConverter BoolToVisibility = new System.Windows.Controls.BooleanToVisibilityConverter();

    static DockingManager()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingManager),
            new FrameworkPropertyMetadata(typeof(DockingManager)));
    }

    /// <summary>Creates the manager.</summary>
    public DockingManager()
    {
        _panes.CollectionChanged += OnPanesChanged;
        InitAutoHide();
    }

    /// <summary>Occurs after a pane was closed (hidden) by the user.</summary>
    public event RoutedEventHandler PaneClosed
    {
        add => AddHandler(DockPane.ClosedEvent, value);
        remove => RemoveHandler(DockPane.ClosedEvent, value);
    }

    /// <summary>The docked panes.</summary>
    public ObservableCollection<DockPane> Panes => _panes;

    /// <summary>The pane whose zone is currently selected (two-way bindable).</summary>
    public DockPane? ActivePane
    {
        get => (DockPane?)GetValue(ActivePaneProperty);
        set => SetValue(ActivePaneProperty, value);
    }

    private DockZoneHost? LeftHost { get; set; }
    private DockZoneGroup? LeftGroup { get; set; }

    private DockZoneHost? TopHost { get; set; }
    private DockZoneGroup? TopGroup { get; set; }

    private DockZoneHost? RightHost { get; set; }
    private DockZoneGroup? RightGroup { get; set; }

    private DockZoneHost? BottomHost { get; set; }
    private DockZoneGroup? BottomGroup { get; set; }

    private DockZoneHost? CenterHost { get; set; }

    private System.Windows.Controls.ColumnDefinition? LeftColumn { get; set; }

    private System.Windows.Controls.ColumnDefinition? RightColumn { get; set; }

    private System.Windows.Controls.RowDefinition? TopRow { get; set; }

    private System.Windows.Controls.RowDefinition? BottomRow { get; set; }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachZoneHandlers();
        LeftHost = GetTemplateChild(PartLeftHost) as DockZoneHost;
        TopHost = GetTemplateChild(PartTopHost) as DockZoneHost;
        RightHost = GetTemplateChild(PartRightHost) as DockZoneHost;
        BottomHost = GetTemplateChild(PartBottomHost) as DockZoneHost;
        CenterHost = GetTemplateChild(PartCenterHost) as DockZoneHost;
        LeftGroup = EnsureGroup(LeftHost, PartLeftHost, DockZone.Left);
        TopGroup = EnsureGroup(TopHost, PartTopHost, DockZone.Top);
        RightGroup = EnsureGroup(RightHost, PartRightHost, DockZone.Right);
        BottomGroup = EnsureGroup(BottomHost, PartBottomHost, DockZone.Bottom);
        LeftColumn = GetTemplateChild(PartLeftColumn) as System.Windows.Controls.ColumnDefinition;
        RightColumn = GetTemplateChild(PartRightColumn) as System.Windows.Controls.ColumnDefinition;
        TopRow = GetTemplateChild(PartTopRow) as System.Windows.Controls.RowDefinition;
        BottomRow = GetTemplateChild(PartBottomRow) as System.Windows.Controls.RowDefinition;
        AttachZoneHandlers();
        AttachAutoHideOverlay();

        foreach (var pane in _panes)
        {
            SyncPane(pane);
        }
    }

    private void DetachZoneHandlers()
    {
        foreach (var host in new[] { LeftHost, TopHost, RightHost, BottomHost, CenterHost })
        {
            if (host is not null)
            {
                host.SelectionChanged -= OnZoneSelectionChanged;
            }
        }
    }

    private void AttachZoneHandlers()
    {
        foreach (var host in new[] { LeftHost, TopHost, RightHost, BottomHost, CenterHost })
        {
            if (host is not null)
            {
                host.SelectionChanged -= OnZoneSelectionChanged;
                host.SelectionChanged += OnZoneSelectionChanged;
                host.PaneDragStarted -= OnPaneDragStarted;
                host.PaneDragStarted += OnPaneDragStarted;
            }
        }
    }

    private DockPane? _dragPane;
    private DockDropAdorner? _dragAdorner;

    /// <summary>Called by a zone host when the user drags a pane tab.</summary>
    private void OnPaneDragStarted(object? sender, DockPane pane)
    {
        StartPaneDrag(pane);
    }

    /// <summary>Begins a drag-to-redock operation (mouse is captured by the manager).</summary>
    internal void StartPaneDrag(DockPane pane)
    {
        _dragPane = pane;
        CaptureMouse();
    }

    /// <summary>The zone under the given point (manager coordinates).</summary>
    private static DockZone ZoneAt(Point point, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return DockZone.Center;
        }

        if (point.X < size.Width * 0.25)
        {
            return DockZone.Left;
        }

        if (point.X > size.Width * 0.75)
        {
            return DockZone.Right;
        }

        if (point.Y < size.Height * 0.25)
        {
            return DockZone.Top;
        }

        if (point.Y > size.Height * 0.75)
        {
            return DockZone.Bottom;
        }

        return DockZone.Center;
    }

    private void ShowDropAdorner(DockZone zone)
    {
        var layer = AdornerLayer.GetAdornerLayer(this);
        if (layer is null)
        {
            return;
        }

        if (_dragAdorner is not null)
        {
            layer.Remove(_dragAdorner);
            _dragAdorner = null;
        }

        var fill = TryFindResource(VCWpfControls.Grid.GridResources.SelectionBackgroundKey) as Brush ?? Brushes.DodgerBlue;
        var border = TryFindResource(VCWpfControls.Grid.GridResources.AccentKey) as Brush ?? Brushes.DodgerBlue;
        _dragAdorner = new DockDropAdorner(this, zone, fill, border);
        layer.Add(_dragAdorner);
    }

    private void ClearDropAdorner()
    {
        if (_dragAdorner is null)
        {
            return;
        }

        AdornerLayer.GetAdornerLayer(this)?.Remove(_dragAdorner);
        _dragAdorner = null;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragPane is null)
        {
            return;
        }

        ShowDropAdorner(ZoneAt(e.GetPosition(this), RenderSize));
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragPane is null)
        {
            return;
        }

        var zone = ZoneAt(e.GetPosition(this), RenderSize);
        var pane = _dragPane;
        _dragPane = null;
        ReleaseMouseCapture();
        ClearDropAdorner();

        if (pane.Zone != zone)
        {
            pane.Zone = zone; // SyncPane moves it
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _dragPane = null;
        ClearDropAdorner();
    }

    /// <summary>Test hook: whether the given zone currently hosts visible panes.</summary>
    /// <summary>Test hook: the left zone's split-group container.</summary>
    internal DockZoneGroup? LeftGroupForTest() => LeftGroup;

    /// <summary>Test hook: the concatenated pane headers of the zone's host index.</summary>
    internal string HostCaptionsForTest(int hostIndex)
    {
        var host = LeftGroup?.Hosts.Count > hostIndex ? LeftGroup.Hosts[hostIndex] : null;
        return string.Join("|", host?.Panes.Select(p => p.Header?.ToString() ?? string.Empty) ?? Enumerable.Empty<string>());
    }

    internal bool HasVisiblePanesForTest(DockZone zone)
        => zone == DockZone.Center
            ? CenterHost is { HasVisiblePanes: true }
            : HostsOf(zone).Any(h => h.HasVisiblePanes);

    /// <summary>Test access to the zone hit-testing.</summary>
    internal DockZone ZoneAtForTest(Point point) => ZoneAt(point, RenderSize);

    /// <summary>Test hook: applies a drop at the given manager-relative point.</summary>
    internal void CompletePaneDragAtForTest(Point point)
    {
        if (_dragPane is null)
        {
            return;
        }

        var zone = ZoneAt(point, RenderSize);
        var pane = _dragPane;
        _dragPane = null;
        ClearDropAdorner();
        if (pane.Zone != zone)
        {
            pane.Zone = zone;
        }
    }

    private void OnPanesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var pane in e.OldItems?.Cast<DockPane>() ?? Array.Empty<DockPane>())
        {
            pane.Manager = null;
            foreach (var host in Zones)
            {
                host.Panes.Remove(pane);
            }

            RemoveFromAutoHideCollections(pane);
        }

        foreach (var pane in e.NewItems?.Cast<DockPane>() ?? Array.Empty<DockPane>())
        {
            pane.Manager = this;
            SyncPane(pane);
        }

        RefreshZoneVisibility();
    }

    /// <summary>All zone hosts (left..center).</summary>
    private IEnumerable<DockZoneHost> Zones
    {
        get
        {
            if (LeftHost is not null) yield return LeftHost;
            if (TopHost is not null) yield return TopHost;
            if (RightHost is not null) yield return RightHost;
            if (BottomHost is not null) yield return BottomHost;
            if (CenterHost is not null) yield return CenterHost;
        }
    }

    /// <summary>Places the pane into the zone matching <see cref="DockPane.Zone"/>.</summary>
    /// <summary>
    /// Wraps the template's zone host into a <see cref="DockZoneGroup"/> placed at the same
    /// grid cell: nested splitter groups live INSIDE the group, the first host is the
    /// template's one.
    /// </summary>
    private static DockZoneGroup? EnsureGroup(DockZoneHost? templateHost, string partName, DockZone zone)
    {
        if (templateHost is null)
        {
            return null;
        }

        var group = new DockZoneGroup
        {
            Orientation = zone is DockZone.Left or DockZone.Right ? Orientation.Vertical : Orientation.Horizontal,
        };
        var row = System.Windows.Controls.Grid.GetRow(templateHost);
        var column = System.Windows.Controls.Grid.GetColumn(templateHost);
        var parent = templateHost.Parent as System.Windows.Controls.Grid;
        if (parent is not null)
        {
            var index = parent.Children.IndexOf(templateHost);
            parent.Children.Remove(templateHost);
            group.Hosts.Add(templateHost);
            System.Windows.Controls.Grid.SetRow(group, row);
            System.Windows.Controls.Grid.SetColumn(group, column);
            parent.Children.Insert(index, group);
        }
        else
        {
            group.Hosts.Add(templateHost);
        }

        return group;
    }

    /// <summary>All hosts of the zone (the group's hosts, or the single template host).</summary>
    private IEnumerable<DockZoneHost> HostsOf(DockZone zone)
        => ZoneGroupOf(zone)?.Hosts ?? (zone switch
        {
            DockZone.Left => LeftHost is null ? Enumerable.Empty<DockZoneHost>() : new[] { LeftHost },
            DockZone.Top => TopHost is null ? Enumerable.Empty<DockZoneHost>() : new[] { TopHost },
            DockZone.Right => RightHost is null ? Enumerable.Empty<DockZoneHost>() : new[] { RightHost },
            DockZone.Bottom => BottomHost is null ? Enumerable.Empty<DockZoneHost>() : new[] { BottomHost },
            _ => CenterHost is null ? Enumerable.Empty<DockZoneHost>() : new[] { CenterHost },
        });

    private DockZoneGroup? ZoneGroupOf(DockZone zone) => zone switch
    {
        DockZone.Left => LeftGroup,
        DockZone.Top => TopGroup,
        DockZone.Right => RightGroup,
        DockZone.Bottom => BottomGroup,
        _ => null,
    };

    /// <summary>The host of the pane's zone+split group, creating one for a new group.</summary>
    private DockZoneHost? TargetHostOf(DockPane pane)
    {
        var zone = pane.Zone;
        var group = ZoneGroupOf(zone);
        if (group is null || pane.SplitGroup == 0)
        {
            // Group 0 is the primary (template) host — center panes never split.
            return zone switch
            {
                DockZone.Left => LeftHost,
                DockZone.Top => TopHost,
                DockZone.Right => RightHost,
                DockZone.Bottom => BottomHost,
                _ => CenterHost,
            };
        }

        while (group.Hosts.Count <= pane.SplitGroup)
        {
            group.Hosts.Add(new DockZoneHost());
        }

        return group.Hosts[pane.SplitGroup];
    }

    internal void SyncPane(DockPane pane)
    {
        foreach (var host in AllHosts())
        {
            host.Panes.Remove(pane);
        }

        PruneEmptyGroupHosts();
        RemoveFromAutoHideCollections(pane);

        if (pane.IsHidden)
        {
            CloseAutoHideFlyout();
            if (_floatingWindows.Remove(pane, out var closed))
            {
                CloseFloatingWindow(closed);
            }

            RefreshZoneVisibility();
            return;
        }

        if (pane.IsFloating)
        {
            EnsureFloatingWindow(pane);
            RefreshZoneVisibility();
            return;
        }

        if (_floatingWindows.Remove(pane, out var dockedBack))
        {
            // Programmatic dock-back: close the window without triggering the user-close path.
            CloseFloatingWindow(dockedBack);
        }

        if (pane.IsAutoHidden && pane.Zone is not DockZone.Center)
        {
            // Collapsed to an edge tab: the flyout overlay shows it on demand.
            CloseAutoHideFlyout();
            AutoHiddenCollectionOf(pane.Zone).Add(pane);
            RefreshZoneVisibility();
            return;
        }

        var target = TargetHostOf(pane);

        if (target is null)
        {
            return;
        }

        CloseAutoHideFlyout();
        target.Panes.Add(pane);
        target.Select(pane);
        ApplyZoneSize(pane, target);
        RefreshZoneVisibility();
    }

    /// <summary>Shows a hidden pane again (docking it back to its zone).</summary>
    public void ShowPane(DockPane pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        pane.Manager = this;
        pane.IsHidden = false;
    }

    /// <summary>Hides a pane (same as the close button).</summary>
    public void HidePane(DockPane pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        pane.Manager = this;
        pane.IsHidden = true;
    }

    private void ApplyZoneSize(DockPane pane, DockZoneHost target)
    {
        if (ReferenceEquals(target, LeftHost) && LeftColumn is not null)
        {
            LeftColumn.Width = pane.DockWidth;
        }
        else if (ReferenceEquals(target, RightHost) && RightColumn is not null)
        {
            RightColumn.Width = pane.DockWidth;
        }
        else if (ReferenceEquals(target, TopHost) && TopRow is not null)
        {
            TopRow.Height = pane.DockHeight;
        }
        else if (ReferenceEquals(target, BottomHost) && BottomRow is not null)
        {
            BottomRow.Height = pane.DockHeight;
        }
    }

    /// <summary>Collapses empty zones (row/column width 0) so the center fills the space.</summary>
    private void RefreshZoneVisibility()
    {
        var leftVisible = HostsOf(DockZone.Left).Any(h => h.HasVisiblePanes);
        var rightVisible = HostsOf(DockZone.Right).Any(h => h.HasVisiblePanes);
        var topVisible = HostsOf(DockZone.Top).Any(h => h.HasVisiblePanes);
        var bottomVisible = HostsOf(DockZone.Bottom).Any(h => h.HasVisiblePanes);
        if (LeftColumn is not null)
        {
            LeftColumn.Width = leftVisible ? LeftColumn.Width : new GridLength(0);
        }

        if (RightColumn is not null)
        {
            RightColumn.Width = rightVisible ? RightColumn.Width : new GridLength(0);
        }

        if (TopRow is not null)
        {
            TopRow.Height = topVisible ? TopRow.Height : new GridLength(0);
        }

        if (BottomRow is not null)
        {
            BottomRow.Height = bottomVisible ? BottomRow.Height : new GridLength(0);
        }

        if (ActivePane is null or { IsHidden: true })
        {
            var visible = Zones.SelectMany(z => z.Panes).FirstOrDefault();
            if (visible is not null)
            {
                ZoneOf(visible)?.Select(visible);
            }
        }
    }

    /// <summary>Every zone host in the layout, including nested split-group hosts.</summary>
    private IEnumerable<DockZoneHost> AllHosts()
    {
        foreach (var zone in new[] { DockZone.Left, DockZone.Top, DockZone.Right, DockZone.Bottom, DockZone.Center })
        {
            foreach (var host in HostsOf(zone))
            {
                yield return host;
            }
        }
    }

    /// <summary>Removes empty non-primary hosts so closed groups disappear.</summary>
    private void PruneEmptyGroupHosts()
    {
        foreach (var group in new[] { LeftGroup, TopGroup, RightGroup, BottomGroup })
        {
            if (group is null)
            {
                continue;
            }

            for (var i = group.Hosts.Count - 1; i >= 1; i--)
            {
                if (group.Hosts[i].Panes.Count == 0)
                {
                    group.Hosts.RemoveAt(i);
                }
            }
        }
    }

    private DockZoneHost? ZoneOf(DockPane pane)
    {
        foreach (var host in Zones)
        {
            if (host.Panes.Contains(pane))
            {
                return host;
            }
        }

        return null;
    }

    /// <summary>Test access to the floating windows.</summary>
    internal List<Window> FloatingWindowsForTest() => _floatingWindows.Values.ToList();

    /// <summary>Test access to the zone hosts.</summary>
    internal DockZoneHost LeftHostForTest() => LeftHost!;

    /// <summary>Test access to the zone hosts.</summary>
    internal DockZoneHost RightHostForTest() => RightHost!;

    /// <summary>Test access to the zone hosts.</summary>
    internal DockZoneHost TopHostForTest() => TopHost!;

    /// <summary>Test access to the zone hosts.</summary>
    internal DockZoneHost BottomHostForTest() => BottomHost!;

    /// <summary>Test access to the zone hosts.</summary>
    internal DockZoneHost CenterHostForTest() => CenterHost!;

    private void OnZoneSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is DockZoneHost { SelectedPane: { } pane })
        {
            SetCurrentValue(ActivePaneProperty, pane);
        }
    }

    private static void OnActivePaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var manager = (DockingManager)d;
        if (e.NewValue is DockPane pane)
        {
            manager.ZoneOf(pane)?.Select(pane);
        }
    }

    /// <summary>Moves the pane into a floating window above the manager's window.</summary>
    public void FloatPane(DockPane pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        pane.Manager = this;
        pane.IsHidden = false;
        pane.IsFloating = true; // SyncPane detaches it from zones and opens the window
    }

    /// <summary>Docks a floating pane back into its zone.</summary>
    public void DockPaneBack(DockPane pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        pane.Manager = this;
        pane.IsFloating = false;
    }

    private void EnsureFloatingWindow(DockPane pane)
    {
        if (_floatingWindows.ContainsKey(pane))
        {
            return;
        }

        var window = new Window
        {
            Title = Convert.ToString(pane.Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty,
            Content = pane,
            Width = 420,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Owner = Window.GetWindow(this),
            ShowInTaskbar = false,
        };

        // The pane is already detached at this point, so cascade the window over the manager.
        var cascade = 24 * (_floatingWindows.Count % 6);
        var topLeft = PointToScreen(new Point(0, 0));
        window.Left = topLeft.X + 40 + cascade;
        window.Top = topLeft.Y + 40 + cascade;

        window.Closing += OnFloatingWindowClosing;
        _floatingWindows[pane] = window;
        window.Show();
    }

    private void OnFloatingWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is Window { Content: DockPane pane } window)
        {
            window.Closing -= OnFloatingWindowClosing;
            if (_floatingWindows.Remove(pane))
            {
                pane.IsFloating = false; // docks back; no window teardown (it is closing already)
            }
        }
    }

    private void CloseFloatingWindow(Window window)
    {
        window.Closing -= OnFloatingWindowClosing;
        window.Close();
    }

    /// <summary>Serializes the current layout to a compact XML string.</summary>
    public string SaveLayout()
    {
        var model = new DockLayoutModel();
        foreach (var pane in Panes)
        {
            var zone = ZoneOf(pane);
            var entry = new DockPaneLayout
            {
                Name = pane.LayoutKey,
                Zone = pane.Zone,
                IsHidden = pane.IsHidden,
                IsAutoHidden = pane.IsAutoHidden,
                SplitGroup = pane.SplitGroup,
                IsFloating = pane.IsFloating,
                Width = ResolveSize(pane, zone, horizontal: true),
                Height = ResolveSize(pane, zone, horizontal: false),
            };

            if (pane.IsFloating && _floatingWindows.TryGetValue(pane, out var floating))
            {
                entry.FloatingLeft = floating.Left;
                entry.FloatingTop = floating.Top;
                entry.FloatingWidth = floating.Width;
                entry.FloatingHeight = floating.Height;
            }

            model.Panes.Add(entry);
        }

        return DockLayoutSerializer.ToXml(model);
    }

    private double ResolveSize(DockPane pane, DockZoneHost? zone, bool horizontal)
    {
        // Prefer the live zone geometry (as resized via splitters); fall back to the pane's preference.
        if (horizontal)
        {
            if (ReferenceEquals(zone, LeftHost) && LeftColumn is not null)
            {
                return LeftColumn.Width.Value;
            }

            if (ReferenceEquals(zone, RightHost) && RightColumn is not null)
            {
                return RightColumn.Width.Value;
            }

            return pane.DockWidth.Value;
        }

        if (ReferenceEquals(zone, TopHost) && TopRow is not null)
        {
            return TopRow.Height.Value;
        }

        if (ReferenceEquals(zone, BottomHost) && BottomRow is not null)
        {
            return BottomRow.Height.Value;
        }

        return pane.DockHeight.Value;
    }

    /// <summary>Applies an XML layout produced by <see cref="SaveLayout"/>.</summary>
    public void LoadLayout(string xml)
    {
        var model = DockLayoutSerializer.FromXml(xml);
        foreach (var entry in model.Panes)
        {
            var pane = Panes.FirstOrDefault(p =>
                string.Equals(p.LayoutKey, entry.Name, StringComparison.Ordinal));
            if (pane is null)
            {
                continue;
            }

            pane.Zone = entry.Zone;
            pane.SplitGroup = entry.SplitGroup;
            pane.IsAutoHidden = entry.IsAutoHidden;
            pane.IsHidden = entry.IsHidden;
            if (entry.IsFloating)
            {
                pane.Manager = this;
                pane.IsFloating = true;
                if (_floatingWindows.TryGetValue(pane, out var window))
                {
                    window.Left = entry.FloatingLeft;
                    window.Top = entry.FloatingTop;
                    if (entry.FloatingWidth > 0)
                    {
                        window.Width = entry.FloatingWidth;
                    }

                    if (entry.FloatingHeight > 0)
                    {
                        window.Height = entry.FloatingHeight;
                    }
                }
            }
            if (entry.Width > 0)
            {
                pane.DockWidth = new GridLength(entry.Width);
            }

            if (entry.Height > 0)
            {
                pane.DockHeight = new GridLength(entry.Height);
            }
        }
    }
}
