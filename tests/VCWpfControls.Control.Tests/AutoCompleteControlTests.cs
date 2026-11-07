using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Editors;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>Control-level behavior of the AutoComplete editor (template parts + keys).</summary>
public class AutoCompleteControlTests
{
    private static AutoComplete Create(out Window window, int minimumPrefix = 1)
    {
        var control = new AutoComplete
        {
            Width = 240,
            ItemsSource = new[] { "Moscow", "Munich", "Minsk", "Milan" },
            MinimumPrefixLength = minimumPrefix,
        };
        window = TestWindowHost.Show(control);
        TestWindowHost.Pump();
        return control;
    }

    private static void Type(AutoComplete control, string text)
    {
        var box = control.Template.FindName("PART_TextBox", control) as TextBox ?? throw new InvalidOperationException("text box part missing");
        box.Text = text;
        TestWindowHost.Pump();
    }

    private static Popup PopupOf(AutoComplete control)
        => (Popup)control.Template.FindName("PART_Popup", control);

    [StaFact]
    public void Typing_OpensPopupWithMatches()
    {
        var control = Create(out var window);
        try
        {
            Type(control, "mi");
            var popup = PopupOf(control);
            Assert.True(popup.IsOpen);
            var list = (ListBox)control.Template.FindName("PART_List", control);
            Assert.Equal(2, list.Items.Count); // Minsk, Milan
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NoMatches_ClosesPopup()
    {
        var control = Create(out var window);
        try
        {
            Type(control, "mi");
            Assert.True(PopupOf(control).IsOpen);
            Type(control, "zzz");
            Assert.False(PopupOf(control).IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enter_AcceptsSuggestion_RaisesEvent()
    {
        var control = Create(out var window);
        var accepted = 0;
        control.SuggestionAccepted += (_, _) => accepted++;
        try
        {
            Type(control, "mi");
            var box = (TextBox)control.Template.FindName("PART_TextBox", control);
            // Down selects the second suggestion: with "mi" -> Minsk(0), Milan(1).
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromDependencyObject(box) ?? throw new InvalidOperationException("no source"), 0, Key.Down)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
                Source = box,
            });
            TestWindowHost.Pump();
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                PresentationSource.FromDependencyObject(box) ?? throw new InvalidOperationException("no source"), 0, Key.Enter)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
                Source = box,
            });
            TestWindowHost.Pump();

            Assert.Equal(1, accepted);
            Assert.Equal("Milan", control.Text); // second suggestion after ArrowDown
            Assert.Equal("Milan", control.SelectedItem);
            Assert.False(PopupOf(control).IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Escape_ClosesPopup_KeepsText()
    {
        var control = Create(out var window);
        try
        {
            Type(control, "mi");
            var box = (TextBox)control.Template.FindName("PART_TextBox", control);
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(box), 0, Key.Escape)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
            });
            TestWindowHost.Pump();
            Assert.False(PopupOf(control).IsOpen);
            Assert.Null(control.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BelowMinimumPrefix_PopupStaysClosed()
    {
        var control = Create(out var window, minimumPrefix: 3);
        try
        {
            Type(control, "mo"); // below 3
            Assert.False(PopupOf(control).IsOpen);
            Type(control, "mos");
            Assert.True(PopupOf(control).IsOpen);
        }
        finally
        {
            window.Close();
        }
    }
}


/// <summary>The grid column wiring: display text + editing via the AutoComplete editor.</summary>
public class GridAutoCompleteColumnTests
{
    [StaFact]
    public void Column_EditsThroughAutoCompleteEditor()
    {
        var grid = new VCWpfControls.Grid.GridControl { Width = 380, RowHeight = 26, HeaderHeight = 26 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new VCWpfControls.Grid.Columns.GridAutoCompleteColumn
        {
            FieldName = "Department",
            Header = "Dept",
            Width = 180,
            ItemsSource = new[] { "Moscow", "Munich", "Minsk" },
        });
        grid.ItemsSource = new[]
        {
            new VCWpfControls.Control.Tests.TestModels.Employee { Name = "a", Department = "Minsk" },
        };
        TestWindowHost.Pump();

        try
        {
            var row = (VCWpfControls.Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var panel = row.Template.FindName("PART_Cells", row) as VCWpfControls.Grid.Primitives.GridCellsPanel ?? throw new System.InvalidOperationException("cells panel missing");
            var display = panel.GetCellElement(grid.Columns[0]) as TextBlock;
            Assert.NotNull(display);
            Assert.Equal("Minsk", display!.Text);

            grid.SetCurrentCellForTest(row.DataItem, grid.Columns[0]);
            Assert.True(grid.BeginEdit());
            Assert.IsType<VCWpfControls.Editors.AutoComplete>(panel.EditingElement);
            grid.CancelEdit();
        }
        finally
        {
            window.Close();
        }
    }
}
