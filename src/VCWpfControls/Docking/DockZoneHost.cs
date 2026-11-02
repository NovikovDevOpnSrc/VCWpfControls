using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace VCWpfControls.Docking;

/// <summary>
/// One docking zone: a tab strip of its <see cref="DockPane"/> headers plus the content of the
/// selected pane. Built in code (like the group panel) so it needs no extra templates.
/// </summary>
public sealed class DockZoneHost : ContentControl
{
    /// <summary>Identifies the <see cref="SelectedPane"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedPaneProperty = DependencyProperty.Register(
        nameof(SelectedPane), typeof(DockPane), typeof(DockZoneHost),
        new PropertyMetadata(null, (d, e) => ((DockZoneHost)d).OnSelectedPaneChanged(e)));

    /// <summary>Identifies the read-only <see cref="HasVisiblePanes"/> dependency property.</summary>
    private static readonly DependencyPropertyKey HasVisiblePanesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasVisiblePanes), typeof(bool), typeof(DockZoneHost), new PropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasVisiblePanes"/> dependency property.</summary>
    public static readonly DependencyProperty HasVisiblePanesProperty = HasVisiblePanesPropertyKey.DependencyProperty;

    private readonly ObservableCollection<DockPane> _panes = new();
    private readonly StackPanel _strip;
    private readonly ContentPresenter _content;
    private readonly Dictionary<DockPane, ToggleButton> _buttons = new();

    /// <summary>Creates the zone host.</summary>
    public DockZoneHost()
    {
        _panes.CollectionChanged += OnPanesChanged;
        _strip = new StackPanel { Orientation = Orientation.Horizontal };

        _content = new ContentPresenter();

        var root = new System.Windows.Controls.Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        System.Windows.Controls.Grid.SetRow(_strip, 0);
        System.Windows.Controls.Grid.SetRow(_content, 1);
        root.Children.Add(_strip);
        root.Children.Add(_content);
        Content = root;
    }

    /// <summary>The panes currently docked into this zone (order = tab order).</summary>
    public ObservableCollection<DockPane> Panes => _panes;

    /// <summary>The selected (visible) pane of the zone.</summary>
    public DockPane? SelectedPane
    {
        get => (DockPane?)GetValue(SelectedPaneProperty);
        set => SetValue(SelectedPaneProperty, value);
    }

    /// <summary>Whether at least one pane is docked (controls the zone visibility).</summary>
    public bool HasVisiblePanes => (bool)GetValue(HasVisiblePanesProperty);

    /// <summary>Raised when the zone selection changed (bubbles the pane up to the manager).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised when the user starts dragging a pane tab (for drag-to-redock).</summary>
    public event EventHandler<DockPane>? PaneDragStarted;

    private Point _dragStart;
    private bool _pressed;

    /// <summary>Wires mouse-drag detection on a tab button.</summary>
    private void HookDrag(ToggleButton toggle, DockPane pane)
    {
        toggle.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _dragStart = e.GetPosition(this);
            _pressed = true;
        };
        toggle.MouseMove += (_, e) =>
        {
            if (!_pressed)
            {
                return;
            }

            var delta = e.GetPosition(this) - _dragStart;
            if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4)
            {
                return;
            }

            _pressed = false;
            PaneDragStarted?.Invoke(this, pane);
        };
        toggle.MouseLeftButtonUp += (_, _) => _pressed = false;
    }

    private void OnPanesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var pane in e.OldItems?.Cast<DockPane>() ?? Array.Empty<DockPane>())
        {
            if (_buttons.Remove(pane, out var button))
            {
                _strip.Children.Remove(button);
            }
        }

        foreach (var pane in e.NewItems?.Cast<DockPane>() ?? Array.Empty<DockPane>())
        {
            _strip.Children.Add(CreateTabButton(pane));
        }

        SetValue(HasVisiblePanesPropertyKey, _panes.Count > 0);

        if (SelectedPane is null || !_panes.Contains(SelectedPane))
        {
            SelectedPane = _panes.FirstOrDefault();
        }
    }

    private UIElement CreateTabButton(DockPane pane)
    {
        var header = new TextBlock
        {
            Text = Convert.ToString(pane.Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(header);

        if (pane.IsClosable)
        {
            var close = new Button
            {
                Content = "\u2715",
                Focusable = false,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(2),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Tag = pane,
            };
            close.Click += OnCloseClick;
            panel.Children.Add(close);
        }

        var toggle = new ToggleButton
        {
            Content = panel,
            Focusable = false,
            Padding = new Thickness(10, 4, 10, 4),
            BorderThickness = new Thickness(0, 0, 1, 1),
            Tag = pane,
        };
        toggle.Checked += (_, _) => Select(pane);
        HookDrag(toggle, pane);
        _buttons[pane] = toggle;
        return toggle;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DockPane pane })
        {
            pane.Close();
            e.Handled = true;
        }
    }

    /// <summary>Selects the pane (and updates the tab buttons).</summary>
    public void Select(DockPane pane)
    {
        if (_panes.Contains(pane))
        {
            SelectedPane = pane;
        }
    }

    private void OnSelectedPaneChanged(DependencyPropertyChangedEventArgs e)
    {
        foreach (var (pane, button) in _buttons)
        {
            button.IsChecked = ReferenceEquals(pane, SelectedPane);
        }

        _content.Content = SelectedPane?.Content;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
