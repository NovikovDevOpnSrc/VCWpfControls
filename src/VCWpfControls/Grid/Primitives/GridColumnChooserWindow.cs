using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The column chooser dialog: visibility checkboxes in the current column order plus
/// move up/down buttons for reordering. Built in code (no template) so it works in any
/// application; lives in the library's default styles only through system chrome.
/// </summary>
public sealed class GridColumnChooserWindow : Window
{
    private readonly GridControl _owner;
    private readonly ListBox _list;

    /// <summary>Creates the chooser for the given grid.</summary>
    public GridColumnChooserWindow(GridControl owner)
    {
        _owner = owner;
        Title = GridStrings.ColumnChooserTitle;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SizeToContent = SizeToContent.Height;
        Width = 340;
        MinWidth = 260;
        Owner = Window.GetWindow(owner);

        _list = new ListBox { SelectionMode = SelectionMode.Single };
        foreach (var column in owner.Columns)
        {
            var item = new CheckBox
            {
                Content = GridColumnBase.HeaderText(column.Header) ?? column.FieldName,
                IsChecked = column.Visible,
                Tag = column,
                Margin = new Thickness(4, 6, 4, 6),
            };
            item.Checked += (_, _) => column.Visible = true;
            item.Unchecked += (_, _) => column.Visible = false;
            _list.Items.Add(item);
        }

        var up = new Button { Content = GridStrings.MoveUp, Padding = new Thickness(12, 4, 12, 4), MinWidth = 96 };
        up.Click += (_, _) => MoveSelected(-1);

        var down = new Button { Content = GridStrings.MoveDown, Padding = new Thickness(12, 4, 12, 4), MinWidth = 96 };
        down.Click += (_, _) => MoveSelected(1);

        var close = new Button { Content = GridStrings.Close, Padding = new Thickness(12, 4, 12, 4), MinWidth = 96 };
        close.Click += (_, _) => Close();

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };
        buttons.Children.Add(up);
        buttons.Children.Add(down);
        buttons.Children.Add(close);

        var root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(_list);
        Content = root;
    }

    /// <summary>The visibility checkboxes in list order; Tag carries the column (for tests).</summary>
    internal System.Collections.Generic.IEnumerable<CheckBox> GetColumnCheckboxes()
        => _list.Items.OfType<CheckBox>();

    /// <summary>Selects the list entry of the given column (for tests).</summary>
    internal void SelectColumn(GridColumnBase column)
    {
        var box = _list.Items.OfType<CheckBox>().FirstOrDefault(b => ReferenceEquals(b.Tag, column));
        if (box is not null)
        {
            _list.SelectedItem = box;
        }
    }

    /// <summary>Invokes the move up/down action (for tests).</summary>
    internal void MoveSelectedForTest(int delta) => MoveSelected(delta);

    private void MoveSelected(int delta)
    {
        if (_list.SelectedItem is not CheckBox { Tag: GridColumnBase column })
        {
            return;
        }

        var index = _owner.Columns.IndexOf(column);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _owner.Columns.Count)
        {
            return;
        }

        _owner.Columns.RemoveAt(index);
        _owner.Columns.Insert(target, column);

        // Keep the same entry selected after the list rebuild.
        if (0 <= target && target < _list.Items.Count && _list.Items[target] is CheckBox refreshed)
        {
            _list.SelectedItem = refreshed;
        }
    }
}
