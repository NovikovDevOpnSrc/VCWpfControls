using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The optional group panel above the headers: drop target for column headers being dragged
/// (adds a <see cref="GridGroupDescription"/>) and shows one removable chip per active
/// grouping level.
/// </summary>
public sealed class GridGroupPanel : Control
{
    private const string PartChipsHost = "PART_ChipsHost";
    private const string PartHint = "PART_Hint";

    /// <summary>Identifies the <see cref="IsDropTarget"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropTargetProperty = DependencyProperty.Register(
        nameof(IsDropTarget), typeof(bool), typeof(GridGroupPanel), new PropertyMetadata(false));

    private ItemsControl? _chipsHost;
    private TextBlock? _hint;

    static GridGroupPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GridGroupPanel),
            new FrameworkPropertyMetadata(typeof(GridGroupPanel)));
    }

    /// <summary>Whether a column header is currently dragged over the panel.</summary>
    public bool IsDropTarget
    {
        get => (bool)GetValue(IsDropTargetProperty);
        internal set => SetValue(IsDropTargetProperty, value);
    }

    /// <summary>The owning grid, attached when the control template is applied.</summary>
    internal GridControl? Owner { get; set; }

    internal bool HasChips => Owner is not null && Owner.GroupDescriptions.Count > 0;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _chipsHost = GetTemplateChild(PartChipsHost) as ItemsControl;
        _hint = GetTemplateChild(PartHint) as TextBlock;
        EnsureChips();
    }

    /// <summary>Rebuilds the group chips from the owning grid's <c>GroupDescriptions</c>.</summary>
    internal void EnsureChips()
    {
        if (Owner is null || _chipsHost is null)
        {
            return;
        }

        var children = _chipsHost.Items;
        children.Clear();

        foreach (var description in Owner.GroupDescriptions)
        {
            children.Add(CreateChip(description));
        }

        if (_hint is not null)
        {
            _hint.Visibility = Owner.GroupDescriptions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private object CreateChip(GridGroupDescription description)
    {
        var owner = Owner!;
        var title = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = ResolveColumnHeader(description.FieldName),
        };

        var remove = new Button
        {
            Content = "\u2715",
            Focusable = false,
            Cursor = Cursors.Hand,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(2),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = description,
        };
        remove.Click += OnChipRemoveClick;

        var chip = new Border
        {
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { title, remove },
            },
            Background = Brushes.Transparent,
            Tag = description,
        };
        return chip;
    }

    private void OnChipRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GridGroupDescription description } && Owner is not null)
        {
            Owner.GroupDescriptions.Remove(description);
        }
    }

    private string ResolveColumnHeader(string fieldName)
    {
        if (Owner is not null)
        {
            foreach (var column in Owner.Columns)
            {
                if (string.Equals(column.EffectiveSortPath, fieldName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToString(column.Header, System.Globalization.CultureInfo.CurrentCulture) ?? fieldName;
                }
            }
        }

        return fieldName;
    }
}
