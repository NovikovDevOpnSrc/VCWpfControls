using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Docking;

/// <summary>
/// One dockable panel of a <see cref="DockingManager"/>: a header, closable content and a
/// docking zone (left/top/right/bottom/center). Center panes are tabbed.
/// </summary>
public class DockPane : HeaderedContentControl
{
    /// <summary>Identifies the <see cref="Zone"/> dependency property.</summary>
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone), typeof(DockZone), typeof(DockPane),
        new PropertyMetadata(DockZone.Center, OnZoneChanged));

    /// <summary>Identifies the <see cref="IsClosable"/> dependency property.</summary>
    public static readonly DependencyProperty IsClosableProperty = DependencyProperty.Register(
        nameof(IsClosable), typeof(bool), typeof(DockPane), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsHidden"/> dependency property.</summary>
    public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(
        nameof(IsHidden), typeof(bool), typeof(DockPane),
        new PropertyMetadata(false, OnHiddenChanged));

    /// <summary>Identifies the <see cref="DockWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DockWidthProperty = DependencyProperty.Register(
        nameof(DockWidth), typeof(GridLength), typeof(DockPane), new PropertyMetadata(new GridLength(250)));

    /// <summary>Identifies the <see cref="DockHeight"/> dependency property.</summary>
    public static readonly DependencyProperty DockHeightProperty = DependencyProperty.Register(
        nameof(DockHeight), typeof(GridLength), typeof(DockPane), new PropertyMetadata(new GridLength(220)));

    /// <summary>Identifies the <see cref="SplitGroup"/> dependency property.</summary>
    public static readonly DependencyProperty SplitGroupProperty = DependencyProperty.Register(
        nameof(SplitGroup), typeof(int), typeof(DockPane),
        new PropertyMetadata(0, OnSplitGroupChanged));

    /// <summary>Identifies the <see cref="IsAutoHidden"/> dependency property.</summary>
    public static readonly DependencyProperty IsAutoHiddenProperty = DependencyProperty.Register(
        nameof(IsAutoHidden), typeof(bool), typeof(DockPane),
        new PropertyMetadata(false, OnAutoHiddenChanged));

    /// <summary>Identifies the <see cref="IsFloating"/> dependency property.</summary>
    public static readonly DependencyProperty IsFloatingProperty = DependencyProperty.Register(
        nameof(IsFloating), typeof(bool), typeof(DockPane),
        new PropertyMetadata(false, OnFloatingChanged));

    /// <summary>Identifies the <see cref="Closed"/> routed event.</summary>
    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DockPane));

    static DockPane()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockPane),
            new FrameworkPropertyMetadata(typeof(DockPane)));
        FocusableProperty.OverrideMetadata(typeof(DockPane), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Occurs after the pane was closed by the user (now hidden).</summary>
    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    /// <summary>
    /// Whether the pane is collapsed to an edge tab (auto-hide): hovering/clicking the tab
    /// flies the pane out as an overlay; the pin button in the flyout docks it back.
    /// Center panes ignore auto-hide.
    /// </summary>
    public bool IsAutoHidden
    {
        get => (bool)GetValue(IsAutoHiddenProperty);
        set => SetValue(IsAutoHiddenProperty, value);
    }

    private static void OnAutoHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pane = (DockPane)d;
        if (pane.IsAutoHidden && pane.Zone == DockZone.Center)
        {
            // Center panes cannot auto-hide: revert without re-syncing.
            pane.SetCurrentValue(IsAutoHiddenProperty, false);
            return;
        }

        pane.Manager?.SyncPane(pane);
    }

    /// <summary>The docking zone of the pane.</summary>
    public DockZone Zone
    {
        get => (DockZone)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }

    /// <summary>Whether the pane shows a close button.</summary>
    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    /// <summary>Whether the pane is currently hidden (closed but kept in the layout).</summary>
    public bool IsHidden
    {
        get => (bool)GetValue(IsHiddenProperty);
        set => SetValue(IsHiddenProperty, value);
    }

    /// <summary>Preferred width of left/right zones hosting this pane.</summary>
    public GridLength DockWidth
    {
        get => (GridLength)GetValue(DockWidthProperty);
        set => SetValue(DockWidthProperty, value);
    }

    /// <summary>Preferred height of top/bottom zones hosting this pane.</summary>
    public GridLength DockHeight
    {
        get => (GridLength)GetValue(DockHeightProperty);
        set => SetValue(DockHeightProperty, value);
    }

    /// <summary>Whether the pane currently lives in a floating window.</summary>
    public bool IsFloating
    {
        get => (bool)GetValue(IsFloatingProperty);
        internal set => SetValue(IsFloatingProperty, value);
    }

    private static void OnFloatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DockPane)d).Manager?.SyncPane((DockPane)d);

    /// <summary>The owning manager, set when the pane is added to <see cref="DockingManager.Panes"/>.</summary>
    internal DockingManager? Manager { get; set; }

    /// <summary>Closes (hides) the pane when <see cref="IsClosable"/> allows it.</summary>
    public void Close()
    {
        if (!IsClosable || IsHidden)
        {
            return;
        }

        var args = new RoutedEventArgs(ClosedEvent) { Source = this };
        RaiseEvent(args);
        IsHidden = true;
        // The pane detaches from the tree while hiding; re-raise at the manager so
        // handlers attached there still receive the bubbled event.
        Manager?.RaiseEvent(args);
    }

    /// <summary>Pane identity for layout persistence: name, falling back to the header.</summary>
    internal string LayoutKey => !string.IsNullOrEmpty(Name)
        ? Name
        : Convert.ToString(Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// The splitter group of the pane WITHIN its zone: panes of one zone sharing a group
    /// are tabbed together in one host; different groups stack with an independent
    /// splitter between them (nested splitter groups).
    /// </summary>
    public int SplitGroup
    {
        get => (int)GetValue(SplitGroupProperty);
        set => SetValue(SplitGroupProperty, value);
    }

    private static void OnSplitGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DockPane)d).Manager?.SyncPane((DockPane)d);

    private static void OnZoneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DockPane)d).Manager?.SyncPane((DockPane)d);

    private static void OnHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DockPane)d).Manager?.SyncPane((DockPane)d);
}
