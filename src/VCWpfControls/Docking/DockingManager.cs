using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace VCWpfControls.Docking;

/// <summary>
/// Root docking container: hosts <see cref="DockPane"/> items in five zones (left/top/right/
/// bottom with resizable splitters, center tabbed). Supports hiding panes and layout
/// persistence via <see cref="SaveLayout"/>/<see cref="LoadLayout"/>.
/// </summary>
[ContentProperty(nameof(Panes))]
public class DockingManager : Control
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

    private DockZoneHost? TopHost { get; set; }

    private DockZoneHost? RightHost { get; set; }

    private DockZoneHost? BottomHost { get; set; }

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
        LeftColumn = GetTemplateChild(PartLeftColumn) as System.Windows.Controls.ColumnDefinition;
        RightColumn = GetTemplateChild(PartRightColumn) as System.Windows.Controls.ColumnDefinition;
        TopRow = GetTemplateChild(PartTopRow) as System.Windows.Controls.RowDefinition;
        BottomRow = GetTemplateChild(PartBottomRow) as System.Windows.Controls.RowDefinition;
        AttachZoneHandlers();

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
            }
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
    internal void SyncPane(DockPane pane)
    {
        foreach (var host in Zones)
        {
            host.Panes.Remove(pane);
        }

        if (pane.IsHidden)
        {
            RefreshZoneVisibility();
            return;
        }

        var target = pane.Zone switch
        {
            DockZone.Left => LeftHost,
            DockZone.Top => TopHost,
            DockZone.Right => RightHost,
            DockZone.Bottom => BottomHost,
            _ => CenterHost,
        };

        if (target is null)
        {
            return;
        }

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
        if (LeftColumn is not null)
        {
            LeftColumn.Width = LeftHost is { HasVisiblePanes: true } ? LeftColumn.Width : new GridLength(0);
        }

        if (RightColumn is not null)
        {
            RightColumn.Width = RightHost is { HasVisiblePanes: true } ? RightColumn.Width : new GridLength(0);
        }

        if (TopRow is not null)
        {
            TopRow.Height = TopHost is { HasVisiblePanes: true } ? TopRow.Height : new GridLength(0);
        }

        if (BottomRow is not null)
        {
            BottomRow.Height = BottomHost is { HasVisiblePanes: true } ? BottomRow.Height : new GridLength(0);
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
                Width = ResolveSize(pane, zone, horizontal: true),
                Height = ResolveSize(pane, zone, horizontal: false),
            };

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
            pane.IsHidden = entry.IsHidden;
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
